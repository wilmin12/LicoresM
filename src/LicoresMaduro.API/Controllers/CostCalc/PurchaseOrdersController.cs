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
    private readonly DhwDbContext _dhw;
    public PurchaseOrdersController(DhwDbContext dhw) { _dhw = dhw; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? warehouse = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _dhw.PoHeaders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(warehouse))
            q = q.Where(x => x.PhWhse == warehouse);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.PhPoNo.Contains(search) || (x.PhOvrNo != null && x.PhOvrNo.Contains(search)) || (x.PhConNo != null && x.PhConNo.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.PhOrdt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<DhwPoHeader>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{warehouse}/{poNo}")]
    public async Task<IActionResult> GetById(string warehouse, string poNo, CancellationToken ct)
    {
        var header = await _dhw.PoHeaders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhWhse == warehouse && x.PhPoNo == poNo, ct);
        if (header is null)
            return NotFound(ApiResponse.Fail($"PO {poNo} not found."));

        var details = await _dhw.PoDetails.AsNoTracking()
            .Where(x => x.PdWhse == warehouse && x.PdPoNo == poNo)
            .OrderBy(x => x.PdLine)
            .ToListAsync(ct);

        // Enrich with FOB prices
        var itemCodes = details.Select(d => d.PdItem).Where(c => c != null).Distinct().ToList();
        var fobPrices = await _dhw.ItemFobPrices.AsNoTracking()
            .Where(f => itemCodes.Contains(f.ItCode))
            .ToDictionaryAsync(f => f.ItCode, ct);

        var enriched = details.Select(d => new
        {
            d.PdLine, d.PdItem, d.PdOqty, d.PdRqty, d.PdWeig, d.PdLtrs,
            d.PdCstAmt, d.PdUnit, d.PdBsw, d.PdClas, d.PdBrvr, d.PdBran, d.PdStat,
            FobPrice    = d.PdItem != null && fobPrices.TryGetValue(d.PdItem, out var fob) ? fob.ItPurchasePrice : null,
            Commodity   = d.PdItem != null && fobPrices.TryGetValue(d.PdItem, out var fob2) ? fob2.ItCommodity : null
        });

        return Ok(ApiResponse<object>.Ok(new { Header = header, Lines = enriched }));
    }
}
