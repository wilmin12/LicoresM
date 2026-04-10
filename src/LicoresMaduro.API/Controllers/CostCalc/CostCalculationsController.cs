using LicoresMaduro.API.Data;
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

    public CostCalculationsController(ApplicationDbContext db, DhwDbContext dhw, ILogger<CostCalculationsController> logger, IPermissionService permissions)
    { _db = db; _dhw = dhw; _logger = logger; _permissions = permissions; }

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
        return Ok(ApiResponse<CcCalcHeader>.Ok(calc));
    }

    // ── Create new calculation ────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCalcDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        // Get system defaults
        var sysCfg = await _dhw.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct);

        var header = new CcCalcHeader
        {
            CcCalcDate      = DateTime.UtcNow,
            CcForwarderCode = dto.ForwarderCode,
            CcForwarderName = dto.ForwarderName,
            CcCurrCode      = dto.CurrCode,
            CcCurrRate      = dto.CurrRate,
            CcFreight       = dto.OceanFreight,
            CcTransport     = dto.Transport     ?? sysCfg?.CompTransport,
            CcUnloading     = dto.Unloading     ?? sysCfg?.CompUnloading,
            CcLocalHandling = dto.LocalHandling ?? sysCfg?.CompLocalHandling,
            CcWarehouse     = dto.Warehouse,
            CcStatus        = "DR",
            CcCreatedBy     = User.Identity?.Name,
            CcCreatedAt     = DateTime.UtcNow
        };

        _db.CcCalcHeaders.Add(header);
        await _db.SaveChangesAsync(ct); // get the auto-generated CcCalcNumber

        // Attach POs
        foreach (var poNo in dto.PoNumbers)
        {
            var poHeader = await _dhw.PoHeaders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PhPoNo == poNo, ct);
            if (poHeader is null) continue;

            var poHead = new CcCalcPoHead
            {
                CcphCalcNumber  = header.CcCalcNumber,
                CcphLmPoNo      = poNo,
                CcphVendNo      = poHeader.PhOvrNo,
                CcphWhse        = poHeader.PhWhse,
                CcphCurrCode    = dto.CurrCode,
                CcphCurrRate    = dto.CurrRate,
                CcphWeight      = poHeader.PhWeig,
                CcphTotQty      = poHeader.PhOqt,
                CcphTotAmountFC = poHeader.PhTotAmt,
                CcphStatus      = "DR",
                CcphCreatedBy   = User.Identity?.Name
            };
            _db.CcCalcPoHeads.Add(poHead);
        }

        header.CcTotOrd = dto.PoNumbers.Count;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Cost Calculation {CalcNo} created", header.CcCalcNumber);
        return CreatedAtAction(nameof(GetById), new { id = header.CcCalcNumber },
            ApiResponse<CcCalcHeader>.Ok(header, "Calculation created."));
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

        var sysCfg = await _dhw.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct);

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

        foreach (var poHead in calc.PoHeads)
        {
            // Load PO details from DHW
            var lines = await _dhw.PoDetails.AsNoTracking()
                .Where(x => x.PdPoNo == poHead.CcphLmPoNo)
                .ToListAsync(ct);

            var itemCodes = lines.Select(l => l.PdItem).Where(c => c != null).Distinct().ToList();
            var fobMap    = await _dhw.ItemFobPrices.AsNoTracking()
                .Where(f => itemCodes.Contains(f.ItCode))
                .ToDictionaryAsync(f => f.ItCode, ct);

            // ── Lookup tables ─────────────────────────────────────────────────
            // HS codes → duty/econ/OB rates
            var goodsClass = await _db.CcGoodsClassifications.AsNoTracking()
                .Where(g => g.IsActive && itemCodes.Contains(g.GcItemCode))
                .ToDictionaryAsync(g => g.GcItemCode, ct);
            var hsCodes = goodsClass.Values.Select(g => g.GcHsCode).Distinct().ToList();
            var tariffMap = await _db.CcTariffItems.AsNoTracking()
                .Where(t => t.IsActive && hsCodes.Contains(t.TiHsCode))
                .ToDictionaryAsync(t => t.TiHsCode, ct);
            // Inland tariff rates per HS code
            var inlandTariffMap = await _db.CcInlandTariffs.AsNoTracking()
                .Where(t => t.IsActive && hsCodes.Contains(t.ItHsCode))
                .ToDictionaryAsync(t => t.ItHsCode, ct);
            // Item weights for proportional freight distribution
            var itemWeightMap = await _db.CcItemWeights.AsNoTracking()
                .Where(w => w.IsActive && itemCodes.Contains(w.IwItemCode))
                .ToDictionaryAsync(w => w.IwItemCode, ct);
            // Allowed margins per item (item code takes priority over commodity)
            var fobCommodities = fobMap.Values.Select(f => f.ItCommodity).Where(c => c != null).Distinct().ToList();
            var allowedMargins = await _db.CcAllowedMargins.AsNoTracking()
                .Where(m => m.IsActive && (
                    (m.AmItemCode != null && itemCodes.Contains(m.AmItemCode)) ||
                    (m.AmCommodity != null && fobCommodities.Contains(m.AmCommodity))))
                .ToListAsync(ct);

            // Weight proportion for PO-level charge distribution
            decimal poWeightProp = totalWeight > 0 ? (poHead.CcphWeight ?? 0) / totalWeight : 0;
            decimal poFreight    = (calc.CcFreight ?? 0) * (decimal)(poHead.CcphCurrRate ?? dto.CurrRate ?? 1) * poWeightProp;
            decimal poTransport  = (calc.CcTransport ?? 0) * poWeightProp;
            decimal poUnloading  = (calc.CcUnloading ?? 0) * poWeightProp;
            decimal poLH         = (calc.CcLocalHandling ?? 0) * poWeightProp;
            decimal poShipCharge = totalShipChargesLocal * poWeightProp;

            decimal poQty = poHead.CcphTotQty ?? 0;

            // Calculate total item weight within this PO (for within-PO freight distribution)
            decimal totalItemWeightInPo = lines.Sum(l => {
                var qty = l.PdOqty ?? 0;
                return l.PdItem != null && itemWeightMap.TryGetValue(l.PdItem, out var iw)
                    ? qty * iw.IwWeightCase : 0m;
            });
            bool useItemWeights = totalItemWeightInPo > 0;

            // Remove old details
            _db.CcCalcPoDetails.RemoveRange(poHead.Details);
            poHead.Details.Clear();

            decimal poInsuranceTotal    = 0;
            decimal poFobTotal          = 0;
            decimal poInlandTariffTotal = 0;
            decimal poShipChargesTotal  = 0;

            foreach (var line in lines)
            {
                var qty      = line.PdOqty ?? 0;
                var fobPrice = line.PdItem != null && fobMap.TryGetValue(line.PdItem, out var fob) ? fob.ItPurchasePrice ?? 0 : 0;
                var fobTot   = fobPrice * qty;
                poFobTotal  += fobTot;

                // Proportion: use item weight if available, fallback to qty
                decimal lineProp;
                if (useItemWeights && line.PdItem != null && itemWeightMap.TryGetValue(line.PdItem, out var iw))
                    lineProp = totalItemWeightInPo > 0 ? (qty * iw.IwWeightCase) / totalItemWeightInPo : 0;
                else
                    lineProp = poQty > 0 ? qty / poQty : 0;

                decimal freight   = poFreight   * lineProp;
                decimal inland    = (dto.InlandFreight ?? 0) * lineProp;
                decimal lh        = poLH        * lineProp;
                decimal transport = poTransport * lineProp;
                decimal unloading = poUnloading * lineProp;
                decimal shipChg   = poShipCharge * lineProp;
                poShipChargesTotal += shipChg;

                // Insurance = (FOB + Inland + Freight) * insuranceRate
                decimal insurance = (fobTot + inland + freight) * insuranceRate;
                poInsuranceTotal += insurance;

                // Duties / Econ / OB from HS code tariff rates (0 if duty-free)
                decimal duties = 0, econ = 0, ob = 0;
                if (!isDutyFree && line.PdItem != null
                    && goodsClass.TryGetValue(line.PdItem, out var gc)
                    && tariffMap.TryGetValue(gc.GcHsCode, out var tariff))
                {
                    decimal baseCif = fobTot + inland + freight + insurance;
                    duties = baseCif * tariff.TiDutyRate;
                    econ   = (baseCif + duties) * tariff.TiEconRate;
                    ob     = (baseCif + duties + econ) * tariff.TiObRate;
                }

                // Inland tariff (additional rate applied to CIF, distinct from duty)
                decimal inlandTariff = 0;
                if (!isDutyFree && line.PdItem != null
                    && goodsClass.TryGetValue(line.PdItem, out var gc2)
                    && inlandTariffMap.TryGetValue(gc2.GcHsCode, out var inlTariff))
                {
                    decimal baseCif = fobTot + inland + freight + insurance;
                    inlandTariff = baseCif * inlTariff.ItRate;
                    poInlandTariffTotal += inlandTariff;
                }

                // Final Cost
                decimal finalCost = isDutyFree
                    ? fobTot + inland + freight + lh + insurance + transport + unloading + shipChg
                    : fobTot + inland + freight + lh + duties + econ + ob + inlandTariff + insurance + transport + unloading + shipChg;

                // Selling price
                decimal marginPerc = dto.MarginPerc ?? 0;
                decimal sellingPrice = marginPerc < 100 && marginPerc > 0
                    ? finalCost / (1 - marginPerc / 100)
                    : finalCost;

                // Allowed margin lookup: item code first, then commodity
                CcAllowedMargin? am = null;
                if (line.PdItem != null)
                    am = allowedMargins.FirstOrDefault(m => m.AmItemCode == line.PdItem)
                      ?? (fobMap.TryGetValue(line.PdItem, out var fob2) && fob2.ItCommodity != null
                          ? allowedMargins.FirstOrDefault(m => m.AmCommodity == fob2.ItCommodity && m.AmItemCode == null)
                          : null);

                var detail = new CcCalcPoDetail
                {
                    CcpdCalcNumber    = id,
                    CcpdLmPoNo        = poHead.CcphLmPoNo,
                    CcpdItemNo        = line.PdItem ?? string.Empty,
                    CcpdUnitCase      = (int?)line.PdUnit,
                    CcpdOrdQty        = qty,
                    CcpdFobPrice      = fobPrice,
                    CcpdFobPriceTot   = fobTot,
                    CcpdInlandFreight = inland,
                    CcpdFreight       = freight,
                    CcpdLocalHandl    = lh,
                    CcpdDuties        = duties,
                    CcpdEconSurch     = econ,
                    CcpdOb            = ob,
                    CcpdInlandTariff  = inlandTariff,
                    CcpdShipCharges   = shipChg,
                    CcpdInsurance     = insurance,
                    CcpdTransport     = transport,
                    CcpdUnloading     = unloading,
                    CcpdFinalCost     = finalCost,
                    CcpdWarehouse     = calc.CcWarehouse,
                    CcpdMarginPerc    = marginPerc,
                    CcpdSellingPrice  = sellingPrice,
                    CcpdAllowedMin    = am?.AmMinMargin,
                    CcpdAllowedMax    = am?.AmMaxMargin
                };
                _db.CcCalcPoDetails.Add(detail);
                poHead.Details.Add(detail);
            }

            // Update PO head totals
            poHead.CcphLocalHandling = poLH;
            poHead.CcphFreight       = poFreight;
            poHead.CcphTransport     = poTransport;
            poHead.CcphUnloading     = poUnloading;
            poHead.CcphInsurance     = poInsuranceTotal;
            poHead.CcphTotAmount     = poFobTotal;
            poHead.CcphInlandFreight = dto.InlandFreight ?? 0;
            poHead.CcphShipCharges   = poShipChargesTotal;
            poHead.CcphInlandTariff  = poInlandTariffTotal;
        }

        calc.CcTotQty = calc.PoHeads.Sum(p => p.CcphTotQty ?? 0);
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
        _ = SendCostCalcEmailAsync(calc, "confirmed", CancellationToken.None);
        return Ok(ApiResponse.Ok("Calculation confirmed."));
    }

    [HttpPatch("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "COST_CALCULATIONS", "APPROVE", ct))
            return Forbid();

        var calc = await _db.CcCalcHeaders.Include(x => x.PoHeads).FirstOrDefaultAsync(x => x.CcCalcNumber == id, ct);
        if (calc is null) return NotFound(ApiResponse.Fail($"Calculation {id} not found."));
        if (calc.CcStatus != "CF") return BadRequest(ApiResponse.Fail("Only Confirmed calculations can be approved."));
        calc.CcStatus = "AP";
        foreach (var p in calc.PoHeads) { p.CcphStatus = "AP"; p.CcphApprovedBy = User.Identity?.Name; }
        await _db.SaveChangesAsync(ct);
        _ = SendCostCalcEmailAsync(calc, "approved", CancellationToken.None);
        return Ok(ApiResponse.Ok("Calculation approved."));
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

    // ── Email helper ──────────────────────────────────────────────────────────

    /// <summary>
    /// type = "confirmed" → notifies COSTCALC_PRICE_CALC recipients that a calculation is pending approval.
    /// type = "approved"  → notifies COSTCALC_APPROVED_FINANCIAL recipients that a calculation was approved.
    /// </summary>
    private async Task SendCostCalcEmailAsync(CcCalcHeader calc, string type, CancellationToken ct)
    {
        try
        {
            var cfg = await _db.LmEmailConfig.AsNoTracking().FirstOrDefaultAsync(ct);
            if (cfg is null || !cfg.IsEnabled || string.IsNullOrEmpty(cfg.SmtpHost)) return;

            var moduleKey = type == "confirmed" ? "COSTCALC_PRICE_CALC" : "COSTCALC_APPROVED_FINANCIAL";

            var approverCfg = await _db.ModuleApproverEmails.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaeModuleKey == moduleKey, ct);
            var recipients = approverCfg?.MaeEmails
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList() ?? [];

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
    List<string> PoNumbers
);

public record CalcChargesDto(
    decimal? OceanFreight,
    decimal? InlandFreight,
    decimal? Transport,
    decimal? Unloading,
    decimal? LocalHandling,
    decimal? CurrRate,
    decimal? MarginPerc
);
