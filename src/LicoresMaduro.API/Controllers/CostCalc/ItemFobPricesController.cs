using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/item-fob-prices")]
[Authorize]
[Produces("application/json")]
public sealed class ItemFobPricesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly DhwDbContext         _dhw;

    public ItemFobPricesController(ApplicationDbContext db, DhwDbContext dhw)
    { _db = db; _dhw = dhw; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search   = null,
        [FromQuery] string? supplier = null,
        [FromQuery] string? brand    = null,
        CancellationToken ct = default)
    {
        var fobPrices = await _db.CcItemFobPrices.AsNoTracking()
            .OrderBy(x => x.ItCode).ToListAsync(ct);

        var codes = fobPrices.Select(x => x.ItCode.Trim()).Distinct().ToList();
        // No Trim in WHERE — SQL Server CHAR comparison handles trailing-space padding natively
        var itemDetails = await _dhw.ItemT.AsNoTracking()
            .Where(x => codes.Contains(x.ItItem))
            .ToListAsync(ct);
        var detailMap = itemDetails
            .GroupBy(x => x.ItItem.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var suppliers = await _dhw.Suppliers.AsNoTracking().ToListAsync(ct);
        // GroupBy to avoid duplicate key exception when trimmed codes collide
        var supplierMap = suppliers
            .Where(s => s.Supplier != null)
            .GroupBy(s => s.Supplier!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().SupplierName?.Trim() ?? g.Key,
                StringComparer.OrdinalIgnoreCase);

        IEnumerable<ItemFobPriceEnriched> data = fobPrices.Select(f =>
        {
            detailMap.TryGetValue(f.ItCode.Trim(), out var d);
            var supCode = d?.ItSupCode?.Trim();
            var supName = supCode != null && supplierMap.TryGetValue(supCode, out var sn) ? sn : supCode;
            return new ItemFobPriceEnriched(
                f.ItCode.Trim(),
                d?.ItDesc?.Trim(),
                supCode,
                supName,
                d?.ItBrandCode?.Trim(),
                f.ItPurchasePrice,
                f.ItCommodity?.Trim());
        });

        if (!string.IsNullOrWhiteSpace(search))
            data = data.Where(x =>
                x.ItemCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Commodity?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        if (!string.IsNullOrWhiteSpace(supplier))
            data = data.Where(x => string.Equals(x.SupplierCode, supplier, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(x.SupplierName, supplier, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(brand))
            data = data.Where(x => string.Equals(x.BrandCode, brand, StringComparison.OrdinalIgnoreCase));

        return Ok(ApiResponse<List<ItemFobPriceEnriched>>.Ok(data.ToList()));
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetById(string code, CancellationToken ct)
    {
        var item = await _db.CcItemFobPrices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ItCode == code, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item code '{code}' not found."));
        return Ok(ApiResponse<CcItemFobPrice>.Ok(item));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ItemFobPriceDto dto, CancellationToken ct)
    {
        if (await _db.CcItemFobPrices.AnyAsync(x => x.ItCode == dto.ItemCode, ct))
            return Conflict(ApiResponse.Fail($"Item code '{dto.ItemCode}' already exists."));

        var item = new CcItemFobPrice
        {
            ItCode          = dto.ItemCode.Trim(),
            ItPurchasePrice = dto.PurchasePrice,
            ItCommodity     = dto.Commodity?.Trim()
        };
        _db.CcItemFobPrices.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { code = item.ItCode },
            ApiResponse<CcItemFobPrice>.Ok(item, "Created."));
    }

    [HttpPut("{code}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(string code, [FromBody] ItemFobPriceDto dto, CancellationToken ct)
    {
        var item = await _db.CcItemFobPrices.FirstOrDefaultAsync(x => x.ItCode == code, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item code '{code}' not found."));

        if (item.ItCode != dto.ItemCode.Trim() &&
            await _db.CcItemFobPrices.AnyAsync(x => x.ItCode == dto.ItemCode.Trim(), ct))
            return Conflict(ApiResponse.Fail($"Item code '{dto.ItemCode}' already exists."));

        item.ItCode          = dto.ItemCode.Trim();
        item.ItPurchasePrice = dto.PurchasePrice;
        item.ItCommodity     = dto.Commodity?.Trim();
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CcItemFobPrice>.Ok(item, "Updated."));
    }

    [HttpDelete("{code}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        var item = await _db.CcItemFobPrices.FirstOrDefaultAsync(x => x.ItCode == code, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item code '{code}' not found."));
        _db.CcItemFobPrices.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record ItemFobPriceDto(string ItemCode, decimal? PurchasePrice, string? Commodity);

public record ItemFobPriceEnriched(
    string   ItemCode,
    string?  Description,
    string?  SupplierCode,
    string?  SupplierName,
    string?  BrandCode,
    decimal? FobPrice,
    string?  Commodity);
