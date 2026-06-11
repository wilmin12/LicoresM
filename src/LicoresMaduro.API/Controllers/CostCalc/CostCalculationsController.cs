using ClosedXML.Excel;
using LicoresMaduro.API.Data;
using LicoresMaduro.API.Models.Auth;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/calculations")]
[Authorize]
[Produces("application/json")]
public sealed class CostCalculationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly DhwDbContext         _dhw;
    private readonly ILogger<CostCalculationsController> _logger;
    private readonly IPermissionService   _permissions;
    private readonly IServiceScopeFactory _scopeFactory;

    public CostCalculationsController(ApplicationDbContext db, DhwDbContext dhw,
        ILogger<CostCalculationsController> logger, IPermissionService permissions,
        IServiceScopeFactory scopeFactory)
    { _db = db; _dhw = dhw; _logger = logger; _permissions = permissions; _scopeFactory = scopeFactory; }

    // ── List all calculations ─────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var q = _db.CcCalcHeaders.AsNoTracking().Include(x => x.PoHeads);
        IQueryable<CcCalcHeader> filtered = q;
        if (!string.IsNullOrWhiteSpace(status)) filtered = filtered.Where(x => x.CcStatus == status);
        var total = await filtered.CountAsync(ct);
        var data  = await filtered.OrderByDescending(x => x.CcCreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<CcCalcHeader>.Ok(data, page, pageSize, total));
    }

    // ── Get one calculation ───────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var calc = await _db.CcCalcHeaders
            .Include(x => x.PoHeads).ThenInclude(p => p.Details)
            .FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));

        // Enrich with RANKER_952 (Actual Cost VIP) for all items in this calculation
        var itemCodes = calc.PoHeads
            .SelectMany(p => p.Details)
            .Select(d => d.CcpdItemNo?.Trim())
            .Where(c => c != null)
            .Distinct()
            .ToList();

        var ranker952 = itemCodes.Any()
            ? await _dhw.Ranker952.AsNoTracking()
                .Where(r => itemCodes.Contains(r.Item.Trim()))
                .ToDictionaryAsync(r => r.Item.Trim(), ct)
            : new Dictionary<string, DhwRanker952>();

        var calcConfirmedBy = calc.PoHeads
            .FirstOrDefault(p => !string.IsNullOrEmpty(p.CcphConfirmedBy))?.CcphConfirmedBy;

        return Ok(ApiResponse<object>.Ok(new { Calc = calc, Ranker952 = ranker952, CcCalcConfirmedBy = calcConfirmedBy }));
    }

    // ── Last approved calculation by forwarder (for pre-fill) ────────────────
    [HttpGet("last-approved-forwarder")]
    public async Task<IActionResult> GetLastApprovedByForwarder([FromQuery] string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(ApiResponse.Fail("Forwarder code is required."));

        // 1st try: last approved calculation for this forwarder
        var calc = await _db.CcCalcHeaders
            .AsNoTracking()
            .Include(x => x.PoHeads)
            .Where(x => x.CcForwarderCode == code && x.CcStatus == "AP")
            .OrderByDescending(x => x.CcCalcNumber)
            .FirstOrDefaultAsync(ct);

        // Fallback 1: any status for this forwarder code
        calc ??= await _db.CcCalcHeaders
            .AsNoTracking()
            .Include(x => x.PoHeads)
            .Where(x => x.CcForwarderCode == code)
            .OrderByDescending(x => x.CcCalcNumber)
            .FirstOrDefaultAsync(ct);

        // Fallback 2: match by forwarder name (older calcs stored free-text)
        if (calc is null)
        {
            var ff = await _db.FreightForwarders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.FfCode == code, ct);
            if (ff is not null)
                calc = await _db.CcCalcHeaders
                    .AsNoTracking()
                    .Include(x => x.PoHeads)
                    .Where(x => x.CcForwarderName == ff.FfName)
                    .OrderByDescending(x => x.CcCalcNumber)
                    .FirstOrDefaultAsync(ct);
        }

        if (calc is null)
            return Ok(ApiResponse<object>.Ok(null, "No calculation found for this forwarder."));

        var inlandFreight = calc.PoHeads.FirstOrDefault()?.CcphInlandFreight;

        return Ok(ApiResponse<object>.Ok(new {
            calc.CcForwarderName,
            calc.CcCurrCode,
            calc.CcCurrRate,
            OceanFreight  = calc.CcFreight,
            InlandFreight = inlandFreight
        }, "Last approved calculation found."));
    }

    // ── Create new calculation ────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCalcDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        if (dto.PoEntries is null || dto.PoEntries.Count == 0)
            return BadRequest(ApiResponse.Fail("At least one PO number is required."));

        try
        {
            // Get system defaults (table may not exist in local/dev environment)
            SystemTable? sysCfg = null;
            try { sysCfg = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct); } catch { }

            string? createdBy = User.Identity?.Name?.Trunc(50);

            var header = new CcCalcHeader
            {
                CcCalcDate      = DateTime.UtcNow,
                CcForwarderCode = dto.ForwarderCode?.Trunc(10),
                CcForwarderName = dto.ForwarderName?.Trunc(50),
                CcCurrCode      = dto.CurrCode?.Trunc(3),
                CcCurrRate      = dto.CurrRate,
                CcFreight       = dto.OceanFreight,
                CcTransport     = dto.Transport     ?? sysCfg?.CompTransport,
                CcUnloading     = dto.Unloading     ?? sysCfg?.CompUnloading,
                CcLocalHandling = dto.LocalHandling ?? sysCfg?.CompLocalHandling,
                CcWarehouse     = dto.Warehouse?.Trunc(10),
                CcStatus        = "DR",
                CcCreatedBy     = createdBy,
                CcCreatedAt     = DateTime.UtcNow
            };

            _db.CcCalcHeaders.Add(header);
            await _db.SaveChangesAsync(ct); // get the auto-generated CcCalcNumber

            // Attach POs
            foreach (var entry in dto.PoEntries)
            {
                var poHeader = await _dhw.PoHeaders.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PhPoNo == entry.PoNumber, ct);
                if (poHeader is null)
                {
                    _logger.LogWarning("PO {PoNo} not found in DHW — skipping", entry.PoNumber);
                    continue;
                }

                var effectiveCurrCode = (entry.CurrCode ?? dto.CurrCode)?.Trunc(3);
                var effectiveInvRate  = entry.InvRate  ?? dto.CurrRate;
                var effectiveCustRate = entry.CustRate ?? effectiveInvRate;

                var poHead = new CcCalcPoHead
                {
                    CcphCalcNumber   = header.CcCalcNumber,
                    CcphLmPoNo       = entry.PoNumber.Trunc(15),
                    CcphVendNo       = poHeader.PhOvrNo?.Trunc(20),
                    CcphVendName     = entry.VendorName?.Trunc(100),
                    CcphWhse         = poHeader.PhWhse?.Trunc(10),
                    CcphCurrCode     = effectiveCurrCode,
                    CcphCurrRate     = effectiveInvRate,
                    CcphCurrRateCust = effectiveCustRate,
                    CcphInvNumber    = entry.InvoiceNr?.Trunc(20),
                    CcphInvDate      = entry.InvoiceDate,
                    CcphInlandFreight = entry.InlandFreight ?? dto.InlandFreight,
                    CcphLocalHandling = entry.LocalHandling ?? dto.LocalHandling ?? sysCfg?.CompLocalHandling,
                    CcphTransport    = entry.Transport  ?? dto.Transport  ?? sysCfg?.CompTransport,
                    CcphUnloading    = entry.Unloading  ?? dto.Unloading  ?? sysCfg?.CompUnloading,
                    CcphDiscount     = entry.Discount,
                    CcphSelectedLines = entry.SelectedLines != null && entry.SelectedLines.Any() 
                        ? string.Join(",", entry.SelectedLines) 
                        : null,
                    CcphWeight       = poHeader.PhWeig,
                    CcphTotQty       = poHeader.PhOqt,
                    CcphTotAmountFC  = poHeader.PhTotAmt,
                    CcphStatus       = "DR",
                    CcphCreatedBy    = createdBy
                };
                _db.CcCalcPoHeads.Add(poHead);
            }

            header.CcTotOrd = dto.PoEntries.Count;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Cost Calculation {CalcNo} created", header.CcCalcNumber);
            return Ok(ApiResponse<CcCalcHeader>.Ok(header, "Calculation created."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cost calculation");
            return StatusCode(500, ApiResponse.Fail($"Error creating calculation: {ex.Message}"));
        }
    }

    // ── Update calculation ────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCalcDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var calc = await _db.CcCalcHeaders
            .Include(x => x.PoHeads)
            .FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));
        if (calc.CcStatus == "AP") return BadRequest(ApiResponse.Fail("Cannot update an approved calculation."));

        try
        {
            string? modifiedBy = User.Identity?.Name?.Trunc(50);

            // Update header
            calc.CcForwarderCode = dto.ForwarderCode?.Trunc(10);
            calc.CcForwarderName = dto.ForwarderName?.Trunc(50);
            calc.CcCurrCode      = dto.CurrCode?.Trunc(3);
            calc.CcCurrRate      = dto.CurrRate;
            calc.CcFreight       = dto.OceanFreight;
            if (dto.Transport.HasValue)     calc.CcTransport     = dto.Transport;
            if (dto.Unloading.HasValue)     calc.CcUnloading     = dto.Unloading;
            if (dto.LocalHandling.HasValue) calc.CcLocalHandling = dto.LocalHandling;
            if (dto.Warehouse != null)      calc.CcWarehouse     = dto.Warehouse.Trunc(10);

            // Update POs
            var currentPoNos = calc.PoHeads.Select(p => p.CcphLmPoNo).ToList();
            var incomingPoNos = dto.PoEntries?.Select(e => e.PoNumber).ToList() ?? [];

            // 1. Remove POs not in the new list
            var toRemove = calc.PoHeads.Where(p => !incomingPoNos.Contains(p.CcphLmPoNo)).ToList();
            _db.CcCalcPoHeads.RemoveRange(toRemove);

            // 2. Add or update POs
            SystemTable? sysCfg = null;
            try { sysCfg = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct); } catch { }

            foreach (var entry in dto.PoEntries ?? [])
            {
                var poHead = calc.PoHeads.FirstOrDefault(p => p.CcphLmPoNo == entry.PoNumber);
                bool isNew = poHead == null;

                var poHeader = await _dhw.PoHeaders.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PhPoNo == entry.PoNumber, ct);
                
                if (poHeader == null && isNew) continue;

                var effectiveCurrCode = (entry.CurrCode ?? dto.CurrCode)?.Trunc(3);
                var effectiveInvRate  = entry.InvRate  ?? dto.CurrRate;
                var effectiveCustRate = entry.CustRate ?? effectiveInvRate;

                if (isNew)
                {
                    poHead = new CcCalcPoHead
                    {
                        CcphCalcNumber   = id,
                        CcphLmPoNo       = entry.PoNumber.Trunc(15),
                        CcphVendNo       = poHeader!.PhOvrNo?.Trunc(20),
                        CcphVendName     = entry.VendorName?.Trunc(100),
                        CcphWhse         = poHeader.PhWhse?.Trunc(10),
                        CcphStatus       = "DR",
                        CcphCreatedBy    = modifiedBy
                    };
                    _db.CcCalcPoHeads.Add(poHead);
                }

                poHead!.CcphVendName      = entry.VendorName?.Trunc(100);
                poHead.CcphCurrCode      = effectiveCurrCode;
                poHead.CcphCurrRate      = effectiveInvRate;
                poHead.CcphCurrRateCust  = effectiveCustRate;
                poHead.CcphInvNumber     = entry.InvoiceNr?.Trunc(20);
                poHead.CcphInvDate       = entry.InvoiceDate;
                poHead.CcphInlandFreight = entry.InlandFreight ?? dto.InlandFreight;
                poHead.CcphLocalHandling = entry.LocalHandling ?? dto.LocalHandling ?? sysCfg?.CompLocalHandling;
                poHead.CcphTransport     = entry.Transport  ?? dto.Transport  ?? sysCfg?.CompTransport;
                poHead.CcphUnloading     = entry.Unloading  ?? dto.Unloading  ?? sysCfg?.CompUnloading;
                poHead.CcphDiscount      = entry.Discount;
                poHead.CcphSelectedLines = entry.SelectedLines != null && entry.SelectedLines.Any() 
                    ? string.Join(",", entry.SelectedLines) 
                    : null;
                
                if (poHeader != null)
                {
                    poHead.CcphWeight      = poHeader.PhWeig;
                    poHead.CcphTotQty      = poHeader.PhOqt;
                    poHead.CcphTotAmountFC = poHeader.PhTotAmt;
                }
            }

            calc.CcTotOrd = dto.PoEntries?.Count ?? 0;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Cost Calculation {CalcNo} updated", id);
            return Ok(ApiResponse<CcCalcHeader>.Ok(calc, "Calculation updated."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cost calculation");
            return StatusCode(500, ApiResponse.Fail($"Error updating calculation: {ex.Message}"));
        }
    }

    // ── Run the full cost calculation for a calculation ───────────────────────
    [HttpPost("{id:int}/calculate")]
    public async Task<IActionResult> Calculate(int id, [FromBody] CalcChargesDto dto, CancellationToken ct)
    {
        var calc = await _db.CcCalcHeaders
            .Include(x => x.PoHeads).ThenInclude(p => p.Details)
            .FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));
        if (calc.CcStatus == "AP") return BadRequest(ApiResponse.Fail("Cannot recalculate an approved calculation."));

        SystemTable? sysCfg = null;
        try { sysCfg = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct); } catch { }

        // Apply updated charges if provided
        if (dto.OceanFreight.HasValue)   calc.CcFreight       = dto.OceanFreight;
        if (dto.Transport.HasValue)      calc.CcTransport     = dto.Transport;
        if (dto.Unloading.HasValue)      calc.CcUnloading     = dto.Unloading;
        if (dto.LocalHandling.HasValue)  calc.CcLocalHandling = dto.LocalHandling;

        decimal insuranceRate = sysCfg?.CompInsurance ?? 0;
        bool    isDutyFree    = calc.CcWarehouse?.ToUpper() == "DF";

        decimal totalWeight = calc.PoHeads.Sum(p => p.CcphWeight ?? 0);

        // Load ship charges for this calculation (distributed across all POs by weight)
        var shipCharges = await _db.CcShipCharges.AsNoTracking()
            .Where(s => s.ScCalcNumber == id)
            .ToListAsync(ct);
        decimal totalShipChargesLocal = shipCharges.Sum(s => s.ScAmount * (s.ScRate ?? (decimal)(calc.PoHeads.FirstOrDefault()?.CcphCurrRate ?? dto.CurrRate ?? 1)));

        // Vendor insurance rules (loaded once for the whole calculation)
        var cifVendors = (await _db.CcVendorCifs.AsNoTracking().ToListAsync(ct))
            .Select(v => v.VcifVendor.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fwVendors = (await _db.CcVendorFreightWeights.AsNoTracking().ToListAsync(ct))
            .Select(v => v.VfwVendor.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var poHead in calc.PoHeads)
        {
            // Load PO details from DHW
            var query = _dhw.PoDetails.AsNoTracking()
                .Where(x => x.PdPoNo == poHead.CcphLmPoNo);

            if (!string.IsNullOrEmpty(poHead.CcphSelectedLines))
            {
                var selectedLineNumbers = poHead.CcphSelectedLines.Split(',')
                    .Select(s => decimal.TryParse(s, out var d) ? d : -1)
                    .Where(d => d != -1)
                    .ToList();
                if (selectedLineNumbers.Any())
                {
                    query = query.Where(x => selectedLineNumbers.Contains(x.PdLine));
                }
            }

            var lines = await query.ToListAsync(ct);

            var itemCodes = lines.Select(l => l.PdItem?.Trim()).Where(c => c != null).Distinct().ToList();
            var fobMap    = await _db.CcItemFobPrices.AsNoTracking()
                .Where(f => itemCodes.Contains(f.ItCode))
                .ToDictionaryAsync(f => f.ItCode, ct);

            // ── Lookup tables ─────────────────────────────────────────────────
            // HS codes → duty/econ/OB rates
            var goodsClass = await _db.CcGoodsClassifications.AsNoTracking()
                .Where(g => g.IsActive && itemCodes.Contains(g.GcItemCode))
                .ToDictionaryAsync(g => g.GcItemCode.Trim(), ct);
            var hsCodes = goodsClass.Values.Select(g => g.GcHsCode).Distinct().ToList();
            var tariffMap = (await _db.CcTariffItems.AsNoTracking()
                .Where(t => t.IsActive && hsCodes.Contains(t.Hs6Cod))
                .ToListAsync(ct))
                .GroupBy(t => t.Hs6Cod)
                .ToDictionary(g => g.Key, g => g.First());
            // Inland tariff rates per HS code
            var inlandTariffMap = await _db.CcInlandTariffs.AsNoTracking()
                .Where(t => t.IsActive && hsCodes.Contains(t.ItHsCode))
                .ToDictionaryAsync(t => t.ItHsCode, ct);
            // Item weights for proportional freight distribution
            var itemWeightMap = await _db.CcItemWeights.AsNoTracking()
                .Where(w => w.IsActive && itemCodes.Contains(w.IwItemCode))
                .ToDictionaryAsync(w => w.IwItemCode, ct);

            // ── New Liter & Factor Lookups ────────────────────────────────────
            var itemDescrMap = await _dhw.ItemT.AsNoTracking()
                .Where(i => itemCodes.Contains(i.ItItem.Trim()))
                .ToDictionaryAsync(i => i.ItItem.Trim(), ct);

            var ranker560 = await _dhw.Ranker560.AsNoTracking()
                .Where(r => itemCodes.Contains(r.Field1))
                .ToDictionaryAsync(r => r.Field1, ct);
            var ranker562 = await _dhw.Ranker562.AsNoTracking()
                .Where(r => itemCodes.Contains(r.Field1))
                .ToDictionaryAsync(r => r.Field1, ct);
            var alcExceptions = await _db.CcItemsOnAlcPerc.AsNoTracking()
                .Where(e => itemCodes.Contains(e.IoapItemNo))
                .Select(e => e.IoapItemNo)
                .ToListAsync(ct);
            // ──────────────────────────────────────────────────────────────────
            // Allowed margins per item (item code takes priority over commodity)
            var fobCommodities = fobMap.Values.Select(f => f.ItCommodity).Where(c => c != null).Distinct().ToList();
            var allowedMargins = await _db.CcAllowedMargins.AsNoTracking()
                .Where(m => m.IsActive && (
                    (m.AmItemCode != null && itemCodes.Contains(m.AmItemCode)) ||
                    (m.AmCommodity != null && fobCommodities.Contains(m.AmCommodity))))
                .ToListAsync(ct);

            // Weight proportion for PO-level charge distribution (Container -> PO)
            decimal poWeightProp = totalWeight > 0 ? (poHead.CcphWeight ?? 0) / totalWeight : 0;
            decimal poFreight    = (calc.CcFreight ?? 0) * (decimal)(poHead.CcphCurrRate ?? dto.CurrRate ?? 1) * poWeightProp;
            decimal poTransport  = (calc.CcTransport ?? 0) * poWeightProp;
            decimal poUnloading  = (calc.CcUnloading ?? 0) * poWeightProp;
            decimal poLH         = (calc.CcLocalHandling ?? 0) * poWeightProp;
            decimal poShipCharge = totalShipChargesLocal * poWeightProp;

            // ── PHASE 3: Totals for Value-Based Distribution ──────────────────
            decimal totalFobInPoXcg = 0;
            var lineDataList = new List<(DhwPoDetail Line, decimal FobTot)>();

            decimal currRate = (decimal)(poHead.CcphCurrRate ?? dto.CurrRate ?? 1);
            foreach (var line in lines)
            {
                var fobPriceUsd = line.PdItem != null && fobMap.TryGetValue(line.PdItem.Trim(), out var fob) ? fob.ItPurchasePrice ?? 0 : 0;
                var fobPrice    = fobPriceUsd * currRate;
                var qty         = line.PdItem?.Trim().Length == 6 && (line.PdUnit ?? 0) > 0
                                  ? Math.Ceiling((line.PdOqty ?? 0) / (line.PdUnit ?? 1))
                                  : (line.PdOqty ?? 0);
                var fobTot      = fobPrice * qty;
                totalFobInPoXcg += fobTot;
                lineDataList.Add((line, fobTot));
            }
            // ──────────────────────────────────────────────────────────────────

            decimal poQty = poHead.CcphTotQty ?? 0;

            string? poVendor    = poHead.CcphVendNo?.Trim();
            bool isCifVendor    = !string.IsNullOrEmpty(poVendor) && cifVendors.Contains(poVendor);
            bool isFwVendor     = !string.IsNullOrEmpty(poVendor) && fwVendors.Contains(poVendor);
            bool is11060        = poHead.CcphWhse?.Trim() == "11060";
            const decimal InsFactor = 1.1m * 0.005m * 1.07m;

            // Remove this calc's details (via navigation)
            _db.CcCalcPoDetails.RemoveRange(poHead.Details);
            poHead.Details.Clear();
            // One calculation per PO: also remove any other calc's details for this PO
            var otherCalcDetails = await _db.CcCalcPoDetails
                .Where(d => d.CcpdLmPoNo == poHead.CcphLmPoNo && d.CcpdCalcNumber != id)
                .ToListAsync(ct);
            _db.CcCalcPoDetails.RemoveRange(otherCalcDetails);

            decimal poInsuranceTotal    = 0;
            decimal poFobTotal          = 0;
            decimal poInlandTariffTotal = 0;
            decimal poShipChargesTotal  = 0;
            decimal poLitersTotal       = 0;
            decimal poDiscount          = poHead.CcphDiscount ?? 0;

            // ── Pass 1: per-product pre-econ values ──────────────────────────
            var lineInterms = new List<LineInterm>();
            foreach (var item in lineDataList)
            {
                var line   = item.Line;
                var qty    = line.PdItem?.Trim().Length == 6 && (line.PdUnit ?? 0) > 0
                             ? Math.Ceiling((line.PdOqty ?? 0) / (line.PdUnit ?? 1))
                             : (line.PdOqty ?? 0);
                var free   = line.PdBqty ?? 0;
                var fobTot = item.FobTot;
                var fobPrice = qty > 0 ? fobTot / qty : 0;
                poFobTotal += fobTot;

                // ── Liter & Factor ────────────────────────────────────────────
                decimal factor = 1.0m, ml = 0, liters = 0;
                if (line.PdItem != null)
                {
                    bool isException = alcExceptions.Contains(line.PdItem);
                    if (line.PdItem.StartsWith("1")) // Wine
                    {
                        factor = 1.0m;
                        if (ranker560.TryGetValue(line.PdItem, out var r560))
                            ml = decimal.TryParse(r560.Field2, out var v) ? v : 0;
                    }
                    else if (line.PdItem.StartsWith("2")) // Liquor
                    {
                        if (ranker560.TryGetValue(line.PdItem, out var r560))
                        {
                            ml = decimal.TryParse(r560.Field2, out var v) ? v : 0;
                            decimal proof = decimal.TryParse(r560.Field4, out var p) ? p : 0;
                            decimal proofFactor = proof / 100m;
                            if (isException) factor = proofFactor;
                            else if (ml < 2000) factor = sysCfg?.CompLiterMultiplier ?? 1.2m;
                            else factor = proofFactor;
                        }
                    }
                    else // Beer / others — no factor per Supplementary PDF
                    {
                        factor = 1.0m;
                        if (itemDescrMap.TryGetValue(line.PdItem.Trim(), out var mlDesc))
                            ml = mlDesc.ItMlPerBottle ?? 0;
                    }
                    decimal unitCase = line.PdUnit ?? 1;
                    liters = (qty + free) * unitCase * (ml / 1000m) * factor;
                }
                poLitersTotal += liters;

                // ── Value-based proportions (PO → product) ───────────────────
                decimal lineProp     = totalFobInPoXcg > 0 ? fobTot / totalFobInPoXcg : 0;
                decimal lineDiscount = poDiscount * lineProp;
                decimal netFobTot    = fobTot - lineDiscount;
                // CIF vendors: ocean freight & inland freight must stay 0 (same rule as Insurance below)
                decimal freight      = isCifVendor ? 0 : poFreight * lineProp;
                decimal inland       = isCifVendor ? 0 : (poHead.CcphInlandFreight ?? 0) * (decimal)(poHead.CcphCurrRate ?? dto.CurrRate ?? 1) * lineProp;
                decimal lh           = poLH         * lineProp;
                decimal transport    = poTransport  * lineProp;
                decimal unloading    = poUnloading  * lineProp;
                decimal shipChg      = poShipCharge * lineProp;
                poShipChargesTotal  += shipChg;
                decimal lineWeight   = line.PdWeig ?? 0;

                // ── HS code lookup ────────────────────────────────────────────
                string? hsCode = null;
                string? itemTrimmed = line.PdItem?.Trim();
                if (itemTrimmed != null && goodsClass.TryGetValue(itemTrimmed, out var gcLookup))
                    hsCode = gcLookup.GcHsCode;
                else
                    _logger.LogWarning("[CALC] item={Item} has no GoodsClassification — Duties/Econ/OB will be 0", itemTrimmed);

                // Duties are calculated in the HandelsBenaming group pass below

                // ── Inland tariff ─────────────────────────────────────────────
                decimal inlandTariff = 0;
                if (!isDutyFree && hsCode != null && inlandTariffMap.TryGetValue(hsCode, out var inlTariff))
                {
                    inlandTariff = (netFobTot + inland + freight) * inlTariff.ItRate;
                    poInlandTariffTotal += inlandTariff;
                }

                // ── Factor history (per calculation) ──────────────────────────
                if (line.PdItem != null)
                {
                    var existingFactor = await _db.CcItemLiterFactorCalcFin
                        .FirstOrDefaultAsync(f => f.CalcNumber == id && f.ItemNo == line.PdItem, ct);
                    if (existingFactor == null)
                        _db.CcItemLiterFactorCalcFin.Add(new CcItemLiterFactorCalcFin { CalcNumber = id, ItemNo = line.PdItem, Factor = factor });
                    else { existingFactor.Factor = factor; _db.Entry(existingFactor).State = EntityState.Modified; }
                }

                lineInterms.Add(new LineInterm(
                    line, qty, free, fobPrice, fobTot, netFobTot,
                    inland, freight, lh, transport, unloading, shipChg,
                    inlandTariff, liters, factor, hsCode, lineWeight));
            }

            // Duties: T04 + T06 + T09 + T10, grouped by HS code
            // Distributed proportionally to each line by liter contribution.
            _logger.LogInformation("[DUTIES] isDutyFree={DutyFree} lineInterms={Count} withHsCode={WithHs}",
                isDutyFree, lineInterms.Count, lineInterms.Count(l => l.HsCode != null));
            if (!isDutyFree)
            {
                foreach (var grp in lineInterms.Where(l => l.HsCode != null).GroupBy(l => l.Line.PdItem!.Trim()))
                {
                    string? hsCode = grp.First().HsCode;
                    if (!tariffMap.TryGetValue(hsCode!, out var tariffD)) continue;
                    decimal groupLiters = grp.Sum(l => l.Liters);
                    if (groupLiters == 0m) continue;

                    decimal t04 = tariffD.TarT04 ?? 0m;
                    decimal t06 = tariffD.TarT06 ?? 0m;
                    decimal t09 = tariffD.TarT09 ?? 0m;
                    decimal t10 = tariffD.TarT10 ?? 0m;

                    decimal roundedLiters = CustomsRounding.RoundLiters(groupLiters);
                    decimal hectoliter   = CustomsRounding.LitersToHectoliter(groupLiters);

                    decimal groupDuties = CustomsRounding.CeilTax(hectoliter * t04)
                                        + (roundedLiters * t06 / 100m)
                                        + CustomsRounding.CeilTax(hectoliter * t09)
                                        + CustomsRounding.CeilTax(hectoliter * t10);

                    foreach (var lc in grp)
                    {
                        lc.Duties = groupDuties;
                    }
                    _logger.LogInformation("[DUTIES] item={Item} HS={HS} liters={Liters} HL={HL} " +
                        "T04={T04} T06={T06} T09={T09} T10={T10} duties={Duties}",
                        grp.Key, hsCode, groupLiters, hectoliter,
                        t04, t06, t09, t10, groupDuties);
                }
            }

            // ── Pass 2: Econ & OB per Goederencode group ─────────────────────
            // Econ = (T01 + T02) × Total Aduana  |  OB = (T07/2) × Total Aduana
            // Total Aduana = FOB + Inland + Ocean, grouped by HS code.
            // Both are distributed back to products proportionally.
            foreach (var group in lineInterms.Where(l => l.HsCode != null).GroupBy(l => l.HsCode!))
            {
                if (!tariffMap.TryGetValue(group.Key, out var tariffE)) continue;
                decimal groupAduana = group.Sum(l => l.NetFobTot + l.Inland + l.Freight);
                decimal groupEcon   = ((tariffE.TarT01 ?? 0m) + (tariffE.TarT02 ?? 0m)) / 100m * groupAduana;
                decimal groupOb     = (tariffE.TarT07 ?? 0m) / 2m / 100m * groupAduana;
                _logger.LogInformation("[PASS2] HS={HS} groupAduana={Aduana} T01={T01} T02={T02} T07={T07} econ={Econ} ob={OB}",
                    group.Key, groupAduana, tariffE.TarT01, tariffE.TarT02, tariffE.TarT07, groupEcon, groupOb);
                foreach (var lc in group)
                {
                    decimal lineAduana = lc.NetFobTot + lc.Inland + lc.Freight;
                    decimal prop = groupAduana > 0 ? lineAduana / groupAduana : 0;
                    lc.Econ = groupEcon * prop;
                    lc.Ob   = groupOb   * prop;
                }
            }

            // ── Header insurance for Vendor_Freight_Weight vendors ───────────
            decimal poHeaderInsurance = 0;
            if (!isCifVendor && isFwVendor)
            {
                decimal insBase = is11060
                    ? lineInterms.Sum(l => l.NetFobTot + l.Inland + l.Freight + l.Lh)
                    : lineInterms.Sum(l => l.NetFobTot + l.Inland + l.Freight + l.Lh + l.Duties + l.Econ + l.Ob);
                poHeaderInsurance = insBase * InsFactor;
            }

            // ── Pass 3: create detail records ────────────────────────────────
            foreach (var lc in lineInterms)
            {
                decimal ob = lc.Ob;

                // Insurance per vendor rule
                decimal insurance;
                if (isCifVendor)
                {
                    insurance = 0;
                }
                else if (isFwVendor)
                {
                    decimal poTotWeight = poHead.CcphWeight ?? 0;
                    insurance = poTotWeight > 0 ? (lc.LineWeight / poTotWeight) * poHeaderInsurance : 0;
                }
                else
                {
                    decimal insBase = is11060
                        ? lc.NetFobTot + lc.Inland + lc.Freight + lc.Lh
                        : lc.NetFobTot + lc.Inland + lc.Freight + lc.Lh + lc.Duties + lc.Econ + ob;
                    insurance = insBase * InsFactor;
                }
                poInsuranceTotal += insurance;

                decimal finalCostLine = lc.NetFobTot + lc.Inland + lc.Freight + lc.Lh
                    + lc.Duties + lc.Econ + ob + lc.InlandTariff + insurance
                    + lc.Transport + lc.Unloading + lc.ShipChg;

                decimal marginPerc = dto.MarginPerc ?? 0.3m;
                CcAllowedMargin? margin = null;
                if (lc.Line.PdItem != null)
                    margin = allowedMargins.FirstOrDefault(m => m.AmItemCode == lc.Line.PdItem)
                          ?? (fobMap.TryGetValue(lc.Line.PdItem, out var f2) && f2.ItCommodity != null
                              ? allowedMargins.FirstOrDefault(m => m.AmCommodity == f2.ItCommodity && m.AmItemCode == null)
                              : null);
                if (margin != null) marginPerc = margin.AmDefMargin;

                decimal sellingPrice = marginPerc < 1 && marginPerc > 0
                    ? (lc.Qty > 0 ? (finalCostLine / lc.Qty) / (1 - marginPerc) : 0)
                    : (lc.Qty > 0 ? (finalCostLine / lc.Qty) : 0);

                itemDescrMap.TryGetValue(lc.Line.PdItem?.Trim() ?? "", out var iDesc);
                decimal? mlPerBottle = iDesc?.ItMlPerBottle;
                decimal litersPerBottle = (mlPerBottle ?? 0) / 1000m;

                poHead.Details.Add(new CcCalcPoDetail
                {
                    CcpdCalcNumber    = id,
                    CcpdLmPoNo        = poHead.CcphLmPoNo,
                    CcpdItemNo        = lc.Line.PdItem ?? "N/A",
                    CcpdItemDescr     = iDesc != null
                                            ? $"{iDesc.ItShort?.Trim()} {iDesc.ItDesc?.Trim()}".Trim().Trunc(50)
                                            : lc.Line.PdSitem?.Trunc(50),
                    CcpdUnitCase      = (int?)lc.Line.PdUnit,
                    CcpdUm            = mlPerBottle != null ? litersPerBottle.ToString("0.###") : lc.Line.PdUm?.Trim(),
                    CcpdCLiter        = mlPerBottle != null ? (mlPerBottle.Value / 10m) : null,
                    CcpdMl            = (int?)mlPerBottle,
                    CcpdOrdQty        = lc.Qty,
                    CcpdFreeQty       = lc.Free,
                    CcpdLiters        = mlPerBottle != null ? litersPerBottle : null,
                    CcpdTotLiters     = lc.Liters,
                    CcpdFactor        = Math.Round(lc.Factor, 2),
                    CcpdFobPrice      = lc.FobPrice,
                    CcpdFobPriceTot   = lc.FobTot,
                    CcpdFobPriceUsd   = currRate > 0 ? lc.FobPrice / currRate : 0,
                    CcpdInlandFreight = lc.Inland,
                    CcpdFreight       = lc.Freight,
                    CcpdLocalHandl    = lc.Lh,
                    CcpdDuties        = lc.Duties,
                    CcpdEconSurch     = lc.Econ,
                    CcpdOb            = ob,
                    CcpdInlandTariff  = lc.InlandTariff,
                    CcpdShipCharges   = lc.ShipChg,
                    CcpdInsurance     = insurance,
                    CcpdTransport     = lc.Transport,
                    CcpdUnloading     = lc.Unloading,
                    CcpdFinalCost     = finalCostLine,
                    CcpdWarehouse     = poHead.CcphWhse?.Trim(),
                    CcpdMarginPerc    = marginPerc,
                    CcpdSellingPrice  = sellingPrice,
                    CcpdAllowedMin    = margin?.AmMinMargin,
                    CcpdAllowedMax    = margin?.AmMaxMargin
                });
            }

            // ── Pass 4: Benaming summary — one row per unique item per PO ────────
            // One calculation per PO: replace ALL rows for this PO (any calc)
            var oldPoBenamingSums = await _db.CcCalcPoDetBenamingSums
                .Where(s => s.CcpdsLmPoNo == poHead.CcphLmPoNo)
                .ToListAsync(ct);
            _db.CcCalcPoDetBenamingSums.RemoveRange(oldPoBenamingSums);

            // Recalculate Duties, Econ, OB on grouped totals (customs formula from PDF)
            foreach (var grp in lineInterms.Where(l => l.Line.PdItem != null).GroupBy(l => l.Line.PdItem!.Trim()))
            {
                string  handelsBenam = grp.Key;
                string? goedCode     = grp.First().HsCode;
                tariffMap.TryGetValue(goedCode ?? string.Empty, out var tariffS);

                decimal totInland  = grp.Sum(l => l.Inland);
                decimal totFreight = grp.Sum(l => l.Freight);
                decimal totWaarde  = grp.Sum(l => l.NetFobTot);
                decimal rawLiters  = grp.Sum(l => l.Liters);

                // Supplementary liters rounding (<100: 1 dec ≥6 threshold, ≥100: integer ≥6 threshold)
                decimal totLiters = CustomsRounding.RoundLiters(rawLiters);

                // Valor de Aduana (Douanewaarde) = Inland + Freight + Waarde → ceiling to integer
                decimal valorAduana = CustomsRounding.CeilDouanewaarde(totInland + totFreight + totWaarde);

                // Econ Surcharge = Valor_Aduana × TAR_T01 / 100 → ceiling to 1 decimal
                decimal t01 = tariffS?.TarT01 ?? 0m;
                decimal econSurch = CustomsRounding.CeilTax(valorAduana * t01 / 100m);

                // OB = Valor_Aduana × (TAR_T07 / 2) / 100 → ceiling to 1 decimal
                decimal t07 = tariffS?.TarT07 ?? 0m;
                decimal ob  = CustomsRounding.CeilTax(valorAduana * (t07 / 2m) / 100m);

                // Duties: T06 on rounded liters, T04/T09/T10 on raw HL
                decimal duties = 0;
                if (!isDutyFree && tariffS != null)
                {
                    decimal hectoliter = CustomsRounding.LitersToHectoliter(rawLiters);
                    decimal t04 = tariffS.TarT04 ?? 0m;
                    decimal t06 = tariffS.TarT06 ?? 0m;
                    decimal t09 = tariffS.TarT09 ?? 0m;
                    decimal t10 = tariffS.TarT10 ?? 0m;

                    decimal dutyT04 = CustomsRounding.CeilTax(hectoliter * t04);
                    decimal dutyT06 = totLiters * t06 / 100m;
                    decimal dutyT09 = CustomsRounding.CeilTax(hectoliter * t09);
                    decimal dutyT10 = CustomsRounding.CeilTax(hectoliter * t10);

                    duties = dutyT04 + dutyT06 + dutyT09 + dutyT10;

                    _logger.LogInformation("[CCPDS] item={Item} lines={LineCount} rawLiters={Raw} displayLiters={Display} HL={HL} " +
                        "T04={T04}(duty={DT04}) T06={T06}(duty={DT06}) T09={T09}(duty={DT09}) T10={T10}(duty={DT10}) " +
                        "totalDuties={Duties} valorAduana={Aduana} econ={Econ} ob={OB}",
                        handelsBenam, grp.Count(), rawLiters, totLiters, hectoliter,
                        t04, dutyT04, t06, dutyT06, t09, dutyT09, t10, dutyT10,
                        duties, valorAduana, econSurch, ob);
                }

                _db.CcCalcPoDetBenamingSums.Add(new CcCalcPoDetBenamingSum
                {
                    CcpdsCalcNumber       = id,
                    CcpdsLmPoNo           = poHead.CcphLmPoNo,
                    CcpdsHandelsBenam     = handelsBenam,
                    CcpdsGoedCode         = goedCode,
                    CcpdsOrdQty           = grp.Sum(l => l.Qty),
                    CcpdsCostOrg          = grp.First().FobPrice > 0 ? grp.First().FobPrice / currRate : 0,
                    CcpdsTotInlandFreight = totInland,
                    CcpdsTotFreight       = totFreight,
                    CcpdsTotWaarde        = totWaarde,
                    CcpdsTotLiters        = totLiters,
                    CcpdsDuties           = duties,
                    CcpdsEconSurch        = econSurch,
                    CcpdsOb               = ob,
                    CcpdsTarT01           = tariffS?.TarT01,
                    CcpdsTarT02           = tariffS?.TarT02,
                    CcpdsTarT04           = tariffS?.TarT04,
                    CcpdsTarT05           = tariffS?.TarT05,
                    CcpdsTarT06           = tariffS?.TarT06,
                    CcpdsTarT07           = tariffS?.TarT07,
                    CcpdsTarT08           = tariffS?.TarT08,
                    CcpdsTarT09           = tariffS?.TarT09,
                    CcpdsTarT10           = tariffS?.TarT10,
                    CcpdsTarT12           = tariffS?.TarT12,
                });
            }

            // Update PO head totals
            poHead.CcphLocalHandling = poLH;
            // CIF vendors carry no ocean freight; keep CcphInlandFreight as the entered reference value (detail lines drive the calc)
            poHead.CcphFreight       = isCifVendor ? 0 : poFreight;
            poHead.CcphTransport     = poTransport;
            poHead.CcphUnloading     = poUnloading;
            poHead.CcphInsurance     = poInsuranceTotal;
            poHead.CcphTotAmount     = poFobTotal;
            poHead.CcphShipCharges   = poShipChargesTotal;
            poHead.CcphInlandTariff  = poInlandTariffTotal;
            poHead.CcphTotQty        = poQty;
            poHead.CcphTotLiters     = poLitersTotal;
        }

        calc.CcTotWeight = totalWeight;
        calc.CcTotQty    = calc.PoHeads.Sum(p => p.CcphTotQty ?? 0);
        calc.CcTotOrd    = calc.PoHeads.Count;

        await _db.SaveChangesAsync(ct);

        // Reload with fresh data
        var updated = await _db.CcCalcHeaders
            .Include(x => x.PoHeads).ThenInclude(p => p.Details)
            .FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);

        return Ok(ApiResponse<CcCalcHeader>.Ok(updated!, "Calculation completed."));
    }

    // ── Confirm / Approve ─────────────────────────────────────────────────────
    [HttpPatch("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct)
    {
        var calc = await _db.CcCalcHeaders.Include(x => x.PoHeads).FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));
        if (calc.CcStatus != "DR") return BadRequest(ApiResponse.Fail("Only Draft calculations can be confirmed."));
        calc.CcStatus = "CF";
        foreach (var p in calc.PoHeads) { p.CcphStatus = "CF"; p.CcphConfirmedBy = User.Identity?.Name; }
        await _db.SaveChangesAsync(ct);

        // Pre-load email data while DbContext is still alive
        LmEmailConfig? emailCfg = null;
        List<string> confirmedRecipients = [];
        try
        {
            emailCfg = await _db.LmEmailConfig.AsNoTracking().FirstOrDefaultAsync(ct);
            var approverCfg = await _db.ModuleApproverEmails.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaeModuleKey == "COSTCALC_PRICE_CALC", ct);
            confirmedRecipients = approverCfg?.MaeEmails
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim()).Where(r => !string.IsNullOrEmpty(r)).ToList() ?? [];
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not pre-load email config for Confirm #{Id}", id); }

        _ = SendCostCalcEmailAsync(calc, "confirmed", emailCfg, confirmedRecipients, CancellationToken.None);
        _ = NotifyCostCalcConfirmedAsync(calc, CancellationToken.None);
        return Ok(ApiResponse.Ok("Calculation confirmed."));
    }

    [HttpPatch("{id:int}/return-to-draft")]
    public async Task<IActionResult> ReturnToDraft(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "COST_CALCULATIONS", "EDIT", ct))
            return Forbid();
        var calc = await _db.CcCalcHeaders.Include(x => x.PoHeads).FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));
        if (calc.CcStatus != "CF") return BadRequest(ApiResponse.Fail("Only Confirmed calculations can be returned to Draft."));
        calc.CcStatus = "DR";
        foreach (var p in calc.PoHeads) { p.CcphStatus = "DR"; }
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Calculation {Id} returned to Draft by {User}", id, User.Identity?.Name);
        return Ok(ApiResponse.Ok("Calculation returned to Draft."));
    }

    [HttpPatch("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "COST_CALCULATIONS", "APPROVE", ct))
            return Forbid();

        var calc = await _db.CcCalcHeaders.Include(x => x.PoHeads).FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));
        if (calc.CcStatus != "CF") return BadRequest(ApiResponse.Fail("Only Confirmed calculations can be approved."));

        // 4-eyes: whoever confirmed cannot approve
        var currentUser = User.Identity?.Name ?? string.Empty;
        bool confirmedByCurrentUser = calc.PoHeads
            .Any(p => !string.IsNullOrEmpty(p.CcphConfirmedBy) &&
                      string.Equals(p.CcphConfirmedBy, currentUser, StringComparison.OrdinalIgnoreCase));

        if (confirmedByCurrentUser)
            return BadRequest(new { Message = "The user who confirmed this calculation cannot approve it. Another user must approve." });

        calc.CcStatus = "AP";
        foreach (var p in calc.PoHeads) { p.CcphStatus = "PC"; p.CcphApprovedBy = User.Identity?.Name; }
        await _db.SaveChangesAsync(ct);

        // Fire-and-forget: compute VIP price matrix in background
        var calcIdForBg  = id;
        var approvedByBg = User.Identity?.Name;
        var scopeFactory = _scopeFactory;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IPriceCalculationService>();
                await svc.ComputeAndPersistAsync(calcIdForBg, approvedByBg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PriceCalc background task failed for Calc #{CalcId}", calcIdForBg);
            }
        });

        // Pre-load data needed for background tasks while DbContext is still alive
        var calcWithDetails = await _db.CcCalcHeaders
            .Include(x => x.PoHeads).ThenInclude(p => p.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);

        SystemTable? sysCfg = null;
        try { sysCfg = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct); } catch { }

        // Pre-load email data while DbContext is still alive
        LmEmailConfig? emailCfg = null;
        List<string> approvedRecipients = [];
        try
        {
            emailCfg = await _db.LmEmailConfig.AsNoTracking().FirstOrDefaultAsync(ct);
            var approverCfg = await _db.ModuleApproverEmails.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaeModuleKey == "COSTCALC_APPROVED_FINANCIAL", ct);
            approvedRecipients = approverCfg?.MaeEmails
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim()).Where(r => !string.IsNullOrEmpty(r)).ToList() ?? [];
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not pre-load email config for Approve #{Id}", id); }

        var managerEmail = sysCfg?.CompEmailConfirmMngr?.Trim();

        _ = SendCostCalcEmailAsync(calc, "approved", emailCfg, approvedRecipients, CancellationToken.None);
        _ = SendManagerConfirmEmailAsync(calc, emailCfg, managerEmail, CancellationToken.None);
        if (calcWithDetails is not null)
        {
            _ = GenerateCostChangesExcelAsync(calcWithDetails, sysCfg?.CompPathCostChanges, CancellationToken.None);
            _ = GenerateCostCalcPdfAsync(calcWithDetails, sysCfg?.CompPathCostCalc, CancellationToken.None);
        }
        _logger.LogInformation("Approve #{Id}: PDF folder={Folder}, managerEmail={Mgr}", id, sysCfg?.CompPathCostCalc, managerEmail);
        return Ok(ApiResponse.Ok("Calculation approved."));
    }

    [HttpPatch("{id:int}/confirm-prices")]
    public async Task<IActionResult> ConfirmPrices(int id, [FromBody] List<ConfirmPriceItemDto> items, CancellationToken ct)
    {
        var calc = await _db.CcCalcHeaders.FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));
        if (calc.CcStatus != "AP") return BadRequest(ApiResponse.Fail("Only Approved calculations can have prices confirmed."));

        foreach (var item in items)
        {
            var detail = await _db.CcCalcPoDetails.FirstOrDefaultAsync(
                d => d.CcpdCalcNumber == id && d.CcpdLmPoNo == item.PoNo && d.CcpdItemNo == item.ItemNo, ct);
            if (detail is null) continue;
            detail.CcpdSellingPrice = item.SellingPrice;
        }
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Cost Calc #{Id} — prices confirmed by {User}, {Count} items updated",
            id, User.Identity?.Name, items.Count);
        return Ok(ApiResponse.Ok("Prices confirmed."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var calc = await _db.CcCalcHeaders.Include(x => x.PoHeads).ThenInclude(p => p.Details).FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));
        if (calc.CcStatus == "AP") return BadRequest(ApiResponse.Fail("Cannot delete an approved calculation."));
        _db.CcCalcHeaders.Remove(calc);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }

    // ── In-app notifications ──────────────────────────────────────────────────

    private async Task NotifyCostCalcConfirmedAsync(CcCalcHeader calc, CancellationToken ct)
    {
        try
        {
            var adminRoles = new[] { "Admin", "SuperAdmin" };
            var userIds = await _db.LmUsers.AsNoTracking()
                .Where(u => u.IsActive && u.Role != null && adminRoles.Contains(u.Role.RoleName))
                .Select(u => u.UserId)
                .ToListAsync(ct);

            if (userIds.Count == 0) return;

            var notifications = userIds.Select(uid => new LmNotification
            {
                NtfUserId  = uid,
                NtfTitle   = $"Cost Calculation #{calc.CcCalcNumber} confirmado",
                NtfMessage = $"El cálculo #{calc.CcCalcNumber} fue confirmado y está pendiente de aprobación.",
                NtfType    = "INFO",
                NtfUrl     = $"/pages/cost-calc/calculation.html?id={calc.CcCalcNumber}",
                NtfRefId   = calc.CcCalcNumber,
                NtfRefType = "COST_CALC",
            }).ToList();

            _db.LmNotifications.AddRange(notifications);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Cost Calc #{Id} — {Count} in-app notification(s) created.", calc.CcCalcNumber, notifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create in-app notifications for Cost Calc #{Id}", calc.CcCalcNumber);
        }
    }

    // ── Email helper ──────────────────────────────────────────────────────────

    private async Task SendCostCalcEmailAsync(CcCalcHeader calc, string type, LmEmailConfig? cfg, List<string> recipients, CancellationToken ct)
    {
        await Task.Yield();
        try
        {
            if (cfg is null || !cfg.IsEnabled || string.IsNullOrEmpty(cfg.SmtpHost)) return;
            if (recipients.Count == 0) return;

            var calcTable = $@"
<table style='border-collapse:collapse;font-family:sans-serif;font-size:14px;'>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Calculation #:</td><td>{calc.CcCalcNumber}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Date:</td><td>{calc.CcCalcDate:yyyy-MM-dd}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Forwarder:</td><td>{calc.CcForwarderName}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Currency:</td><td>{calc.CcCurrCode} @ {calc.CcCurrRate:N4}</td></tr>
</table>";

            string subject;
            string body;

            if (type == "confirmed")
            {
                subject = $"[Cost Calculation] #{calc.CcCalcNumber} — Confirmed, Pending Approval";
                body    = $"<p>Cost Calculation <b>#{calc.CcCalcNumber}</b> has been <b style='color:#0d6efd;'>confirmed</b> and is awaiting financial approval.</p>{calcTable}<p>Please log in to review and approve.</p>";
            }
            else
            {
                subject = $"[Cost Calculation] #{calc.CcCalcNumber} — Approved ✔";
                body    = $"<p>Cost Calculation <b>#{calc.CcCalcNumber}</b> has been <b style='color:green;'>approved</b>.</p>{calcTable}";
            }

            using var client = new SmtpClient();
            var sslOption = cfg.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, sslOption, ct);
            await client.AuthenticateAsync(cfg.SenderEmail, cfg.SenderPassword, ct);

            foreach (var to in recipients)
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(cfg.SenderName, cfg.SenderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Body    = new TextPart("html") { Text = body };
                await client.SendAsync(message, ct);
                _logger.LogInformation("Cost Calc #{Id} — email '{Type}' sent to {To}", calc.CcCalcNumber, type, to);
            }

            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send '{Type}' email for Cost Calculation #{Id}", type, calc.CcCalcNumber);
        }
    }

    // Notifies CompEmailConfirmMngr that prices are ready for confirmation.
    private async Task SendManagerConfirmEmailAsync(CcCalcHeader calc, LmEmailConfig? cfg, string? recipient, CancellationToken ct)
    {
        await Task.Yield();
        try
        {
            if (cfg is null || !cfg.IsEnabled || string.IsNullOrEmpty(cfg.SmtpHost)) return;
            if (string.IsNullOrEmpty(recipient)) return;

            var poNos = calc.PoHeads.Select(p => p.CcphLmPoNo).ToList();
            var poList = string.Join(", ", poNos);

            var calcTable = $@"
<table style='border-collapse:collapse;font-family:sans-serif;font-size:14px;'>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Calculation #:</td><td>{calc.CcCalcNumber}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Date:</td><td>{calc.CcCalcDate:yyyy-MM-dd}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Forwarder:</td><td>{calc.CcForwarderName}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Currency:</td><td>{calc.CcCurrCode} @ {calc.CcCurrRate:N4}</td></tr>
  <tr><td style='padding:4px 12px 4px 0;font-weight:bold;'>Purchase Orders:</td><td>{poList}</td></tr>
</table>";

            var subject = $"[Cost Calculation] #{calc.CcCalcNumber} — Price Confirmation Required";
            var body    = $@"<p>Cost Calculation <b>#{calc.CcCalcNumber}</b> has been <b style='color:green;'>approved</b>.</p>
{calcTable}
<p>The calculated selling prices are ready for your review and confirmation.</p>
<p style='margin-top:16px;'>
  <a href='pages/cost-calc/price-confirmation.html?id={calc.CcCalcNumber}'
     style='background:#6b2929;color:#fff;padding:8px 18px;text-decoration:none;border-radius:4px;font-family:sans-serif;'>
    Confirm Prices
  </a>
</p>
<p style='color:#888;font-size:12px;margin-top:16px;'>If the button does not work, log in to the system and navigate to Cost Calculations → Price Confirmation.</p>";

            using var client = new SmtpClient();
            var sslOption = cfg.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, sslOption, ct);
            await client.AuthenticateAsync(cfg.SenderEmail, cfg.SenderPassword, ct);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(cfg.SenderName, cfg.SenderEmail));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body    = new TextPart("html") { Text = body };
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Cost Calc #{Id} — manager confirm email sent to {To}", calc.CcCalcNumber, recipient);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send manager confirm email for Cost Calculation #{Id}", calc.CcCalcNumber);
        }
    }

    // Generates Qry_Cost_Calc_Approved_Fin_VIP_COST Excel and saves to CompPathCostChanges.
    // calc and folder are pre-loaded in the request scope to avoid DbContext disposal issues.
    private async Task GenerateCostChangesExcelAsync(CcCalcHeader calc, string? folder, CancellationToken ct)
    {
        await Task.Yield(); // ensure it runs asynchronously off the request thread
        try
        {
            var calcId = calc.CcCalcNumber;
            if (string.IsNullOrWhiteSpace(folder)) { _logger.LogWarning("Cost Calc #{Id} — COMP_PATH_COST_CHANGES not configured, skipping Excel.", calcId); return; }

            Directory.CreateDirectory(folder);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Cost Calc Detail");

            // ── Header row ────────────────────────────────────────────────────
            string[] headers =
            [
                "PO Number", "Item No", "Description", "Units/Case",
                "Qty", "Free", "Liters", "Factor",
                "FOB Total", "Inland Frt", "Ocean Frt", "Local Hdl",
                "Duties", "Eco Surch", "OB Tax", "Inland Tariff",
                "Ship Chgs", "Insurance", "Transport", "Unloading",
                "Final Cost", "Margin %", "Selling Price"
            ];
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#6b2929");
            headerRow.Style.Font.FontColor = XLColor.White;

            // ── Data rows ─────────────────────────────────────────────────────
            int row = 2;
            foreach (var po in calc.PoHeads.OrderBy(p => p.CcphLmPoNo))
            {
                var details = po.Details.Where(d => (d.CcpdOrdQty ?? 0) > 0).OrderBy(d => d.CcpdItemNo);
                foreach (var d in details)
                {
                    ws.Cell(row, 1).Value  = po.CcphLmPoNo;
                    ws.Cell(row, 2).Value  = d.CcpdItemNo;
                    ws.Cell(row, 3).Value  = d.CcpdItemDescr ?? string.Empty;
                    ws.Cell(row, 4).Value  = (double)(d.CcpdUnitCase  ?? 0);
                    ws.Cell(row, 5).Value  = (double)(d.CcpdOrdQty    ?? 0m);
                    ws.Cell(row, 6).Value  = (double)(d.CcpdFreeQty   ?? 0m);
                    ws.Cell(row, 7).Value  = (double)Math.Round(d.CcpdLiters       ?? 0m, 4);
                    ws.Cell(row, 8).Value  = (double)Math.Round(d.CcpdFactor       ?? 0m, 4);
                    ws.Cell(row, 9).Value  = (double)Math.Round(d.CcpdFobPriceTot  ?? 0m, 2);
                    ws.Cell(row, 10).Value = (double)Math.Round(d.CcpdInlandFreight ?? 0m, 2);
                    ws.Cell(row, 11).Value = (double)Math.Round(d.CcpdFreight       ?? 0m, 2);
                    ws.Cell(row, 12).Value = (double)Math.Round(d.CcpdLocalHandl    ?? 0m, 2);
                    ws.Cell(row, 13).Value = (double)Math.Round(d.CcpdDuties,    2);
                    ws.Cell(row, 14).Value = (double)Math.Round(d.CcpdEconSurch, 2);
                    ws.Cell(row, 15).Value = (double)Math.Round(d.CcpdOb,        2);
                    ws.Cell(row, 16).Value = (double)Math.Round(d.CcpdInlandTariff  ?? 0m, 2);
                    ws.Cell(row, 17).Value = (double)Math.Round(d.CcpdShipCharges   ?? 0m, 2);
                    ws.Cell(row, 18).Value = (double)Math.Round(d.CcpdInsurance     ?? 0m, 2);
                    ws.Cell(row, 19).Value = (double)Math.Round(d.CcpdTransport     ?? 0m, 2);
                    ws.Cell(row, 20).Value = (double)Math.Round(d.CcpdUnloading     ?? 0m, 2);
                    ws.Cell(row, 21).Value = (double)Math.Round(d.CcpdFinalCost     ?? 0m, 2);
                    ws.Cell(row, 22).Value = d.CcpdMarginPerc.HasValue
                        ? (double)Math.Round(d.CcpdMarginPerc.Value * 100m, 2)
                        : 0d;
                    ws.Cell(row, 23).Value = (double)Math.Round(d.CcpdSellingPrice  ?? 0m, 2);
                    row++;
                }
            }

            // ── Format number columns ─────────────────────────────────────────
            if (row > 2)
            {
                var numRange = ws.Range(2, 9, row - 1, 23);
                numRange.Style.NumberFormat.Format = "#,##0.00";
                ws.Range(2, 4, row - 1, 8).Style.NumberFormat.Format = "#,##0.0000";
            }

            ws.Columns().AdjustToContents();

            // ── Calc info sheet ───────────────────────────────────────────────
            var ws2 = wb.AddWorksheet("Calculation Info");
            ws2.Cell(1, 1).Value = "Calculation #";   ws2.Cell(1, 2).Value = calc.CcCalcNumber;
            ws2.Cell(2, 1).Value = "Date";             ws2.Cell(2, 2).Value = calc.CcCalcDate.ToString("yyyy-MM-dd");
            ws2.Cell(3, 1).Value = "Forwarder";        ws2.Cell(3, 2).Value = calc.CcForwarderName ?? string.Empty;
            ws2.Cell(4, 1).Value = "Currency";         ws2.Cell(4, 2).Value = calc.CcCurrCode ?? string.Empty;
            ws2.Cell(5, 1).Value = "Rate";             ws2.Cell(5, 2).Value = calc.CcCurrRate ?? 0;
            ws2.Cell(6, 1).Value = "Warehouse";        ws2.Cell(6, 2).Value = calc.CcWarehouse ?? string.Empty;
            ws2.Cell(7, 1).Value = "Status";           ws2.Cell(7, 2).Value = "AP";
            ws2.Column(1).Style.Font.Bold = true;
            ws2.Columns().AdjustToContents();

            // ── Save ──────────────────────────────────────────────────────────
            // Replace / with _ in calc number (mirrors original filename logic)
            var safeName = calcId.ToString().Replace("/", "_");
            var filePath = Path.Combine(folder, $"CALC_{safeName}.xlsx");
            wb.SaveAs(filePath);

            _logger.LogInformation("Cost Calc #{Id} — Excel saved to {Path}", calcId, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate Cost Changes Excel for Cost Calculation #{Id}", calc.CcCalcNumber);
        }
    }

    // Generates PDF report and saves to CompPathCostCalc.
    private async Task GenerateCostCalcPdfAsync(CcCalcHeader calc, string? folder, CancellationToken ct)
    {
        await Task.Yield();
        try
        {
            if (string.IsNullOrWhiteSpace(folder)) { _logger.LogWarning("Cost Calc #{Id} — COMP_PATH_COST_CALC not configured, skipping PDF.", calc.CcCalcNumber); return; }
            Directory.CreateDirectory(folder);

            QuestPDF.Settings.License = LicenseType.Community;

            static string N(decimal? v, int d = 2) => v.HasValue ? v.Value.ToString($"N{d}") : "–";
            static string S(string? v) => v ?? "–";

            var allDetails = calc.PoHeads.SelectMany(p => p.Details).Where(d => (d.CcpdOrdQty ?? 0) > 0).ToList();
            var totFob     = allDetails.Sum(d => d.CcpdFobPriceTot  ?? 0);
            var totFinal   = allDetails.Sum(d => d.CcpdFinalCost    ?? 0);
            var totSell    = allDetails.Sum(d => d.CcpdSellingPrice  ?? 0);
            var totLiters  = calc.PoHeads.Sum(p => p.CcphTotLiters  ?? 0);
            var totDuties  = allDetails.Sum(d => d.CcpdDuties);
            var totEcon    = allDetails.Sum(d => d.CcpdEconSurch);
            var totOb      = allDetails.Sum(d => d.CcpdOb);
            var totIns     = allDetails.Sum(d => d.CcpdInsurance    ?? 0);
            var totFreight = allDetails.Sum(d => d.CcpdFreight      ?? 0);
            var totInland  = allDetails.Sum(d => d.CcpdInlandFreight ?? 0);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(6.5f).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        col.Spacing(4);

                        // ── Company header ──────────────────────────────────────
                        col.Item().AlignCenter().Text("LICORES MADURO")
                            .FontSize(14).Bold().FontColor(Color.FromHex("#6b2929"));
                        col.Item().AlignCenter().Text("Cost Calculation Report").FontSize(9);
                        col.Item().LineHorizontal(1).LineColor(Color.FromHex("#6b2929"));

                        // ── Calc info ───────────────────────────────────────────
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { for (int i = 0; i < 6; i++) c.RelativeColumn(); });
                            void InfoCell(string label, string value)
                            {
                                t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Column(tc => { tc.Item().Text(label).FontSize(6).FontColor(Colors.Grey.Darken1); tc.Item().Text(value).Bold(); });
                            }
                            InfoCell("Calc #",      calc.CcCalcNumber.ToString());
                            InfoCell("Date",        calc.CcCalcDate.ToString("yyyy-MM-dd"));
                            InfoCell("Status",      "AP — Approved");
                            InfoCell("Warehouse",   S(calc.CcWarehouse));
                            InfoCell("Forwarder",   S(calc.CcForwarderName));
                            InfoCell("Currency",    $"{S(calc.CcCurrCode)} @ {N(calc.CcCurrRate, 4)}");
                            InfoCell("Ocean Frt",   N(calc.CcFreight));
                            InfoCell("Transport",   N(calc.CcTransport));
                            InfoCell("Unloading",   N(calc.CcUnloading));
                            InfoCell("Local Hdl",   N(calc.CcLocalHandling));
                            InfoCell("Total POs",   calc.PoHeads.Count.ToString());
                            InfoCell("Printed",     DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        });

                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        // ── PO sections ─────────────────────────────────────────
                        foreach (var po in calc.PoHeads.OrderBy(p => p.CcphLmPoNo))
                        {
                            var details = po.Details.Where(d => (d.CcpdOrdQty ?? 0) > 0).OrderBy(d => d.CcpdItemNo).ToList();
                            if (!details.Any()) continue;

                            // PO header bar
                            col.Item().Background(Color.FromHex("#f0e8e8")).Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text($"PO: {po.CcphLmPoNo}").Bold().FontSize(7.5f).FontColor(Color.FromHex("#6b2929"));
                                r.RelativeItem().Text($"Vendor: {S(po.CcphVendNo)}").FontSize(7f);
                                r.RelativeItem().Text($"WH: {S(po.CcphWhse)}").FontSize(7f);
                                r.RelativeItem().Text($"Qty: {N(po.CcphTotQty, 0)}").FontSize(7f);
                                r.RelativeItem().Text($"Liters: {N(po.CcphTotLiters, 2)}").FontSize(7f);
                            });

                            // Detail table
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2.2f); // Item No
                                    c.RelativeColumn(1f);   // Qty
                                    c.RelativeColumn(1f);   // Free
                                    c.RelativeColumn(1.5f); // Liters
                                    c.RelativeColumn(2f);   // FOB Tot
                                    c.RelativeColumn(1.5f); // Inland
                                    c.RelativeColumn(1.5f); // Ocean
                                    c.RelativeColumn(1.5f); // LH
                                    c.RelativeColumn(1.5f); // Duties
                                    c.RelativeColumn(1.5f); // Econ
                                    c.RelativeColumn(1.5f); // OB
                                    c.RelativeColumn(1.5f); // Insurance
                                    c.RelativeColumn(1.5f); // Transport
                                    c.RelativeColumn(1.5f); // Unloading
                                    c.RelativeColumn(2f);   // Final Cost
                                    c.RelativeColumn(1.5f); // Margin%
                                    c.RelativeColumn(2f);   // Selling
                                });

                                // Header row
                                void Hdr(string txt) => t.Cell().Background(Color.FromHex("#6b2929"))
                                    .Padding(2).AlignCenter().Text(txt).FontSize(6).Bold().FontColor(Colors.White);

                                Hdr("Item No");  Hdr("Qty");    Hdr("Free");  Hdr("Liters");
                                Hdr("FOB Tot");  Hdr("Inland"); Hdr("Ocean"); Hdr("Local Hdl");
                                Hdr("Duties");   Hdr("Econ");   Hdr("OB");    Hdr("Insurance");
                                Hdr("Transport"); Hdr("Unload"); Hdr("Final Cost"); Hdr("Mgn%"); Hdr("Selling");

                                bool alt = false;
                                foreach (var d in details)
                                {
                                    var bg = alt ? Color.FromHex("#fdf6f6") : Colors.White;
                                    alt = !alt;

                                    void Cell(string v, bool right = true) => t.Cell()
                                        .Background(bg).Border(0.3f).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(2).Element(e => right ? e.AlignRight() : e.AlignLeft())
                                        .Text(v).FontSize(6);

                                    Cell(S(d.CcpdItemNo), false);
                                    Cell(N(d.CcpdOrdQty,    0));
                                    Cell(N(d.CcpdFreeQty,   0));
                                    Cell(N(d.CcpdLiters,    2));
                                    Cell(N(d.CcpdFobPriceTot));
                                    Cell(N(d.CcpdInlandFreight));
                                    Cell(N(d.CcpdFreight));
                                    Cell(N(d.CcpdLocalHandl));
                                    Cell(d.CcpdDuties.ToString("N2"));
                                    Cell(d.CcpdEconSurch.ToString("N2"));
                                    Cell(d.CcpdOb.ToString("N2"));
                                    Cell(N(d.CcpdInsurance));
                                    Cell(N(d.CcpdTransport));
                                    Cell(N(d.CcpdUnloading));
                                    Cell(N(d.CcpdFinalCost));
                                    Cell(d.CcpdMarginPerc.HasValue ? $"{(d.CcpdMarginPerc.Value * 100):N2}%" : "–");
                                    Cell(N(d.CcpdSellingPrice));
                                }

                                // Subtotal row
                                var poFob   = details.Sum(d => d.CcpdFobPriceTot  ?? 0);
                                var poFinal = details.Sum(d => d.CcpdFinalCost    ?? 0);
                                var poSell  = details.Sum(d => d.CcpdSellingPrice ?? 0);
                                void SubCell(string v, bool right = true) => t.Cell()
                                    .Background(Color.FromHex("#e8dede")).Border(0.3f).BorderColor(Colors.Grey.Lighten3)
                                    .Padding(2).Element(e => right ? e.AlignRight() : e.AlignLeft())
                                    .Text(v).FontSize(6).Bold();

                                SubCell("Subtotal", false);
                                SubCell(details.Sum(d => d.CcpdOrdQty ?? 0).ToString("N0"));
                                SubCell("");
                                SubCell(N(po.CcphTotLiters, 2));
                                SubCell(poFob.ToString("N2"));
                                for (int i = 0; i < 10; i++) SubCell("");
                                SubCell(poFinal.ToString("N2"));
                                SubCell("");
                                SubCell(poSell.ToString("N2"));
                            });
                        }

                        col.Item().LineHorizontal(1).LineColor(Color.FromHex("#6b2929"));

                        // ── Grand Summary ────────────────────────────────────────
                        col.Item().Text("Grand Summary").FontSize(8).Bold().FontColor(Color.FromHex("#6b2929"));
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { for (int i = 0; i < 6; i++) c.RelativeColumn(); });
                            void SumCell(string label, string value) =>
                                t.Cell().Background(Color.FromHex("#f8f2f2")).Border(0.5f)
                                    .BorderColor(Colors.Grey.Lighten2).Padding(4).Column(tc =>
                                    {
                                        tc.Item().Text(label).FontSize(6).FontColor(Colors.Grey.Darken1);
                                        tc.Item().Text(value).Bold().FontSize(7);
                                    });

                            SumCell("Total Liters",    totLiters.ToString("N2"));
                            SumCell("Total FOB",       totFob.ToString("N2"));
                            SumCell("Total Inland Frt", totInland.ToString("N2"));
                            SumCell("Total Ocean Frt",  totFreight.ToString("N2"));
                            SumCell("Total Duties",     totDuties.ToString("N2"));
                            SumCell("Total Econ Surch", totEcon.ToString("N2"));
                            SumCell("Total OB Tax",     totOb.ToString("N2"));
                            SumCell("Total Insurance",  totIns.ToString("N2"));

                            t.Cell().ColumnSpan(2).Background(Color.FromHex("#6b2929"))
                                .Border(0.5f).BorderColor(Color.FromHex("#6b2929")).Padding(4).Column(tc =>
                                {
                                    tc.Item().Text("Grand Final Cost").FontSize(6).FontColor(Colors.White).Light();
                                    tc.Item().Text(totFinal.ToString("N2")).Bold().FontSize(9).FontColor(Colors.White);
                                });
                            t.Cell().ColumnSpan(2).Background(Color.FromHex("#1a472a"))
                                .Border(0.5f).BorderColor(Color.FromHex("#1a472a")).Padding(4).Column(tc =>
                                {
                                    tc.Item().Text("Grand Selling Price").FontSize(6).FontColor(Colors.White).Light();
                                    tc.Item().Text(totSell.ToString("N2")).Bold().FontSize(9).FontColor(Colors.White);
                                });
                        });
                    });

                    // Page footer
                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Licores Maduro — Cost Calculation #").FontSize(6).FontColor(Colors.Grey.Medium);
                        x.Span(calc.CcCalcNumber.ToString()).FontSize(6).FontColor(Colors.Grey.Medium);
                        x.Span("    Page ").FontSize(6).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(6).FontColor(Colors.Grey.Medium);
                        x.Span(" / ").FontSize(6).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(6).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            var safeName = calc.CcCalcNumber.ToString().Replace("/", "_");
            var filePath = Path.Combine(folder, $"CALC_{safeName}.pdf");
            doc.GeneratePdf(filePath);

            _logger.LogInformation("Cost Calc #{Id} — PDF saved to {Path}", calc.CcCalcNumber, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate Cost Calc PDF for Calculation #{Id}", calc.CcCalcNumber);
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record CreateCalcDto(
    string? ForwarderCode,
    string? ForwarderName,
    string? CurrCode,
    decimal? CurrRate,
    decimal? OceanFreight,
    decimal? InlandFreight,
    decimal? Transport,
    decimal? Unloading,
    decimal? LocalHandling,
    string? Warehouse,
    List<PoEntryDto> PoEntries
);

public record PoEntryDto(
    string PoNumber,
    string? InvoiceNr,
    DateTime? InvoiceDate,
    string? CurrCode,
    decimal? InvRate,
    decimal? CustRate,
    decimal? InlandFreight,
    decimal? LocalHandling,
    decimal? Transport,
    decimal? Unloading,
    decimal? Discount,
    string? VendorName = null,
    List<decimal>? SelectedLines = null
);

public record ConfirmPriceItemDto(string PoNo, string ItemNo, decimal? SellingPrice);

public record CalcChargesDto(
    decimal? OceanFreight,
    decimal? InlandFreight,
    decimal? Transport,
    decimal? Unloading,
    decimal? LocalHandling,
    decimal? CurrRate,
    decimal? MarginPerc
);

internal static class CustomsRounding
{
    /// <summary>Supplementary liters: &lt;100 → 1 dec (≥6 up), ≥100 → integer (≥6 up)</summary>
    public static decimal RoundLiters(decimal liters)
    {
        if (liters < 100m)
            return Math.Floor(liters * 10m + 0.4m) / 10m;
        return Math.Floor(liters + 0.4m);
    }

    /// <summary>Ceiling liters to integer, then /100</summary>
    public static decimal LitersToHectoliter(decimal liters) =>
        Math.Ceiling(liters) / 100m;

    /// <summary>Douanewaarde: always ceiling to integer</summary>
    public static decimal CeilDouanewaarde(decimal value) =>
        Math.Ceiling(value);

    /// <summary>Taxes (Econ, OB, Duties): always ceiling to 1 decimal</summary>
    public static decimal CeilTax(decimal value) =>
        Math.Ceiling(value * 10m) / 10m;
}

internal sealed class LineInterm(
    LicoresMaduro.API.Data.DhwPoDetail line,
    decimal qty, decimal free,
    decimal fobPrice, decimal fobTot, decimal netFobTot,
    decimal inland, decimal freight, decimal lh,
    decimal transport, decimal unloading, decimal shipChg,
    decimal inlandTariff,
    decimal liters, decimal factor, string? hsCode, decimal lineWeight)
{
    public LicoresMaduro.API.Data.DhwPoDetail Line { get; } = line;
    public decimal Qty          { get; } = qty;
    public decimal Free         { get; } = free;
    public decimal FobPrice     { get; } = fobPrice;
    public decimal FobTot       { get; } = fobTot;
    public decimal NetFobTot    { get; } = netFobTot;
    public decimal Inland       { get; } = inland;
    public decimal Freight      { get; } = freight;
    public decimal Lh           { get; } = lh;
    public decimal Transport    { get; } = transport;
    public decimal Unloading    { get; } = unloading;
    public decimal ShipChg      { get; } = shipChg;
    public decimal Duties       { get; set; }  // set in duties group pass
    public decimal InlandTariff { get; } = inlandTariff;
    public decimal Liters       { get; } = liters;
    public decimal Factor       { get; } = factor;
    public string? HsCode       { get; } = hsCode;
    public decimal LineWeight   { get; } = lineWeight;
    public decimal Econ         { get; set; }  // set in Pass 2
    public decimal Ob           { get; set; }  // set in Pass 2
}
