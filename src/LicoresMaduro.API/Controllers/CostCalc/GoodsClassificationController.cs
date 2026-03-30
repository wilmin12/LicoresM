using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/goods-classification")]
[Authorize]
[Produces("application/json")]
public sealed class GoodsClassificationController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public GoodsClassificationController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var q = _db.CcGoodsClassifications.AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.GcItemCode.Contains(search) || (x.GcItemDescr != null && x.GcItemDescr.Contains(search)) || x.GcHsCode.Contains(search));
        var data = await q.OrderBy(x => x.GcItemCode).ToListAsync(ct);
        return Ok(ApiResponse<List<CcGoodsClassification>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _db.CcGoodsClassifications.AsNoTracking().FirstOrDefaultAsync(x => x.GcId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Goods classification {id} not found."));
        return Ok(ApiResponse<CcGoodsClassification>.Ok(item));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] GoodsClassDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        if (await _db.CcGoodsClassifications.AnyAsync(x => x.GcItemCode == dto.ItemCode, ct))
            return Conflict(ApiResponse.Fail($"Item code '{dto.ItemCode}' already classified."));

        if (!await _db.CcTariffItems.AnyAsync(x => x.TiHsCode == dto.HsCode && x.IsActive, ct))
            return BadRequest(ApiResponse.Fail($"HS Code '{dto.HsCode}' does not exist or is inactive."));

        var item = new CcGoodsClassification
        {
            GcItemCode  = dto.ItemCode,
            GcItemDescr = dto.ItemDescr,
            GcHsCode    = dto.HsCode,
            IsActive    = dto.IsActive ?? true,
            CreatedAt   = DateTime.UtcNow
        };
        _db.CcGoodsClassifications.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.GcId }, ApiResponse<CcGoodsClassification>.Ok(item, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] GoodsClassDto dto, CancellationToken ct)
    {
        var item = await _db.CcGoodsClassifications.FirstOrDefaultAsync(x => x.GcId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Goods classification {id} not found."));

        if (item.GcItemCode != dto.ItemCode && await _db.CcGoodsClassifications.AnyAsync(x => x.GcItemCode == dto.ItemCode, ct))
            return Conflict(ApiResponse.Fail($"Item code '{dto.ItemCode}' already classified."));

        if (!await _db.CcTariffItems.AnyAsync(x => x.TiHsCode == dto.HsCode && x.IsActive, ct))
            return BadRequest(ApiResponse.Fail($"HS Code '{dto.HsCode}' does not exist or is inactive."));

        item.GcItemCode  = dto.ItemCode;
        item.GcItemDescr = dto.ItemDescr;
        item.GcHsCode    = dto.HsCode;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CcGoodsClassification>.Ok(item, "Updated."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await _db.CcGoodsClassifications.FirstOrDefaultAsync(x => x.GcId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Goods classification {id} not found."));
        _db.CcGoodsClassifications.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record GoodsClassDto(
    string  ItemCode,
    string? ItemDescr,
    string  HsCode,
    bool?   IsActive
);
