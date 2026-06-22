using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/purchase-orders")]
[Authorize]
[Produces("application/json")]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly DhwDbContext         _dhw;
    private readonly ApplicationDbContext _db;
    public PurchaseOrdersController(DhwDbContext dhw, ApplicationDbContext db) { _dhw = dhw; _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? warehouse = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool excludeCalculated = false,
        CancellationToken ct = default)
    {
        var q = _dhw.PoHeaders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(warehouse))
            q = q.Where(x => x.PhWhse == warehouse);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.PhPoNo.Contains(search) || (x.PhOvrNo != null && x.PhOvrNo.Contains(search)) || (x.PhConNo != null && x.PhConNo.Contains(search)));

        // Hide POs that already belong to an existing cost calculation so the same
        // PO is not calculated twice. (CcCalcPoHead lives in a separate DbContext,
        // so the excluded set is materialized first then applied to the DHW query.)
        if (excludeCalculated)
        {
            var calculatedPoNos = await _db.CcCalcPoHeads.AsNoTracking()
                .Select(x => x.CcphLmPoNo)
                .Distinct()
                .ToListAsync(ct);
            if (calculatedPoNos.Count > 0)
                q = q.Where(x => !calculatedPoNos.Contains(x.PhPoNo));
        }
        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.PhOrdt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        // Enrich with Forwarder names
        var shipCodes = data.Select(x => x.PhShip?.Trim()).Where(c => c != null).Distinct().ToList();
        var shippers  = shipCodes.Any()
            ? await _dhw.ShipperMasters.AsNoTracking().Where(x => shipCodes.Contains(x.ShipperId)).ToListAsync(ct)
            : new();
        var shipMap = shippers
            .GroupBy(s => s.ShipperId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ShipperName?.Trim() ?? g.Key, StringComparer.OrdinalIgnoreCase);

        // Enrich with Vendor names
        var vendCodes = data.Select(x => x.PhOvrNo?.Trim()).Where(c => c != null).Distinct().ToList();
        var vendors   = vendCodes.Any()
            ? await _dhw.Suppliers.AsNoTracking().Where(x => x.Supplier != null && vendCodes.Contains(x.Supplier.Trim())).ToListAsync(ct)
            : new();
        var vendMap = vendors
            .GroupBy(v => v.Supplier!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().SupplierName?.Trim() ?? g.Key, StringComparer.OrdinalIgnoreCase);

        var enriched = data.Select(h =>
        {
            var fwdCode = h.PhShip?.Trim();
            var fwdName = fwdCode != null && shipMap.TryGetValue(fwdCode, out var sn) ? sn : fwdCode;
            var vndCode = h.PhOvrNo?.Trim();
            var vndName = vndCode != null && vendMap.TryGetValue(vndCode, out var vn) ? vn : vndCode;
            return new PoHeaderEnriched(h, fwdCode, fwdName, vndName);
        }).ToList();

        return Ok(PagedResponse<PoHeaderEnriched>.Ok(enriched, page, pageSize, total));
    }

    [HttpGet("{warehouse}/{**poNo}")]
    public async Task<IActionResult> GetById(string warehouse, string poNo, CancellationToken ct)
    {
        var header = await _dhw.PoHeaders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhWhse == warehouse && x.PhPoNo == poNo, ct);
        
        // Fallback: if not found by WH+PO (maybe WH is truncated), try by PO only
        if (header == null)
        {
            header = await _dhw.PoHeaders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PhPoNo == poNo, ct);
        }

        if (header is null)
            return NotFound(ApiResponse.Fail($"PO {poNo} not found."));

        // Use the actual warehouse from DHW, not the one passed in
        var actualWhse = header.PhWhse;

        // Get Vendor Name
        string? vendorName = header.PhOvrNo?.Trim();
        if (!string.IsNullOrWhiteSpace(vendorName))
        {
            var v = await _dhw.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Supplier == vendorName, ct);
            if (v != null) vendorName = v.SupplierName?.Trim() ?? vendorName;
        }

        var details = await _dhw.PoDetails.AsNoTracking()
            .Where(x => x.PdWhse == actualWhse && x.PdPoNo == poNo)
            .OrderBy(x => x.PdLine)
            .ToListAsync(ct);

        // Enrich with FOB prices
        var itemCodes = details.Select(d => d.PdItem?.Trim()).Where(c => c != null).Distinct().ToList();
        var fobPrices = await _db.CcItemFobPrices.AsNoTracking()
            .Where(f => itemCodes.Contains(f.ItCode))
            .ToDictionaryAsync(f => f.ItCode, ct);

        var enriched = details.Select(d => new
        {
            d.PdLine, d.PdItem, d.PdOqty, d.PdRqty, d.PdWeig, d.PdLtrs,
            d.PdCstAmt, d.PdUnit, d.PdBsw, d.PdClas, d.PdBrvr, d.PdBran, d.PdStat,
            FobPrice    = d.PdItem != null && fobPrices.TryGetValue(d.PdItem.Trim(), out var fob) ? fob.ItPurchasePrice : null,
            Commodity   = d.PdItem != null && fobPrices.TryGetValue(d.PdItem.Trim(), out var fob2) ? fob2.ItCommodity : null
        });

        return Ok(ApiResponse<object>.Ok(new { Header = header, VendorName = vendorName, Lines = enriched }));
    }
}

public record PoHeaderEnriched(DhwPoHeader Header, string? ForwarderCode, string? ForwarderName, string? VendorName)
{
    // Flatten header properties so the JSON shape matches what the frontend already expects
    public string?   PhWhse   => Header.PhWhse;
    public string?   PhPoNo   => Header.PhPoNo;
    public string?   PhBorw   => Header.PhBorw;
    public string?   PhBrvr   => Header.PhBrvr;
    public decimal?  PhOrdt   => Header.PhOrdt;
    public decimal?  PhShdt   => Header.PhShdt;
    public decimal?  PhArdt   => Header.PhArdt;
    public string?   PhConNo  => Header.PhConNo;
    public decimal?  PhOqt    => Header.PhOqt;
    public decimal?  PhWeig   => Header.PhWeig;
    public decimal?  PhLtrs   => Header.PhLtrs;
    public decimal?  PhTotAmt => Header.PhTotAmt;
    public string?   PhOvrNo  => Header.PhOvrNo;
    public string?   PhVendName => VendorName;
    public decimal?  PhLine   => Header.PhLine;
    public string?   PhStat   => Header.PhStat;
}

