using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Stock;

[ApiController]
[Route("api/stock/vendor-constraints")]
[Authorize]
public sealed class StockVendorConstraintsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public StockVendorConstraintsController(ApplicationDbContext db) => _db = db;

    // ── DTOs ─────────────────────────────────────────────────────────────────
    public sealed record StockVendorConstraintsDto(
        string?  FromLocationCode,
        string?  FromLocationName,
        string?  ToLocationCode,
        string?  ToLocationName,
        string?  ShipperCode,
        string?  OrderReviewDay,
        int?     SupplierLeadDays,
        int?     TransitDays,
        int?     WarehouseProcessDays,
        int?     SafetyDays,
        int?     OrderCycleDays,
        decimal? MinOrderQty,
        decimal? OrderIncrement,
        decimal? MinTotalCaseOrder,
        string?  PurchaserName
    );

    // ── GET api/stock/vendor-constraints ──────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? fromLocation,
        [FromQuery] string? toLocation,
        [FromQuery] string? shipper,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var q = _db.StockVendorConstraints.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(fromLocation))
            q = q.Where(x => (x.SvcFromLocationCode ?? "").Contains(fromLocation) ||
                              (x.SvcFromLocationName ?? "").Contains(fromLocation));
        if (!string.IsNullOrWhiteSpace(toLocation))
            q = q.Where(x => (x.SvcToLocationCode ?? "").Contains(toLocation) ||
                              (x.SvcToLocationName ?? "").Contains(toLocation));
        if (!string.IsNullOrWhiteSpace(shipper))
            q = q.Where(x => (x.SvcShipperCode ?? "").Contains(shipper));

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.SvcId)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync();

        return Ok(PagedResponse<StockVendorConstraints>.Ok(items, page, pageSize, total));
    }

    // ── GET api/stock/vendor-constraints/{id} ─────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.StockVendorConstraints.AsNoTracking()
                            .FirstOrDefaultAsync(x => x.SvcId == id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        return Ok(ApiResponse<StockVendorConstraints>.Ok(item));
    }

    // ── POST api/stock/vendor-constraints ─────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StockVendorConstraintsDto dto)
    {
        var entity = MapToEntity(new StockVendorConstraints(), dto);
        _db.StockVendorConstraints.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<StockVendorConstraints>.Ok(entity, "Created."));
    }

    // ── PUT api/stock/vendor-constraints/{id} ─────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StockVendorConstraintsDto dto)
    {
        var entity = await _db.StockVendorConstraints.FindAsync(id);
        if (entity is null) return NotFound(ApiResponse.Fail("Not found."));
        MapToEntity(entity, dto);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<StockVendorConstraints>.Ok(entity, "Updated."));
    }

    // ── DELETE api/stock/vendor-constraints/{id} ──────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.StockVendorConstraints.FindAsync(id);
        if (entity is null) return NotFound(ApiResponse.Fail("Not found."));
        _db.StockVendorConstraints.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Deleted."));
    }

    // ── POST api/stock/vendor-constraints/bulk ────────────────────────────────
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkImport([FromBody] List<StockVendorConstraintsDto> rows)
    {
        // Truncate and re-insert
        _db.StockVendorConstraints.RemoveRange(_db.StockVendorConstraints);
        await _db.SaveChangesAsync();

        var entities = rows.Select(r => MapToEntity(new StockVendorConstraints(), r)).ToList();
        _db.StockVendorConstraints.AddRange(entities);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { ImportedCount = entities.Count }, $"Imported {entities.Count} records."));
    }

    // ── Mapper ────────────────────────────────────────────────────────────────
    private static StockVendorConstraints MapToEntity(StockVendorConstraints e, StockVendorConstraintsDto dto)
    {
        e.SvcFromLocationCode     = dto.FromLocationCode;
        e.SvcFromLocationName     = dto.FromLocationName;
        e.SvcToLocationCode       = dto.ToLocationCode;
        e.SvcToLocationName       = dto.ToLocationName;
        e.SvcShipperCode          = dto.ShipperCode;
        e.SvcOrderReviewDay       = dto.OrderReviewDay;
        e.SvcSupplierLeadDays     = dto.SupplierLeadDays;
        e.SvcTransitDays          = dto.TransitDays;
        e.SvcWarehouseProcessDays = dto.WarehouseProcessDays;
        e.SvcSafetyDays           = dto.SafetyDays;
        e.SvcOrderCycleDays       = dto.OrderCycleDays;
        e.SvcMinOrderQty          = dto.MinOrderQty;
        e.SvcOrderIncrement       = dto.OrderIncrement;
        e.SvcMinTotalCaseOrder    = dto.MinTotalCaseOrder;
        e.SvcPurchaserName        = dto.PurchaserName;
        e.UpdatedAt               = DateTime.UtcNow;
        return e;
    }
}
