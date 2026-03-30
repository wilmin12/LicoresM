using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/item-weights")]
[Authorize]
[Produces("application/json")]
public sealed class ItemWeightsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ItemWeightsController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var q = _db.CcItemWeights.AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.IwItemCode.Contains(search) || (x.IwItemDescr != null && x.IwItemDescr.Contains(search)));
        var data = await q.OrderBy(x => x.IwItemCode).ToListAsync(ct);
        return Ok(ApiResponse<List<CcItemWeight>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _db.CcItemWeights.AsNoTracking().FirstOrDefaultAsync(x => x.IwId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item weight {id} not found."));
        return Ok(ApiResponse<CcItemWeight>.Ok(item));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ItemWeightDto dto, CancellationToken ct)
    {
        if (await _db.CcItemWeights.AnyAsync(x => x.IwItemCode == dto.ItemCode, ct))
            return Conflict(ApiResponse.Fail($"Item code '{dto.ItemCode}' already exists."));
        var item = new CcItemWeight
        {
            IwItemCode   = dto.ItemCode,
            IwItemDescr  = dto.ItemDescr,
            IwWeightCase = dto.WeightCase,
            IwWeightUnit = dto.WeightUnit,
            IsActive     = dto.IsActive ?? true,
            CreatedAt    = DateTime.UtcNow
        };
        _db.CcItemWeights.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.IwId }, ApiResponse<CcItemWeight>.Ok(item, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ItemWeightDto dto, CancellationToken ct)
    {
        var item = await _db.CcItemWeights.FirstOrDefaultAsync(x => x.IwId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item weight {id} not found."));
        if (item.IwItemCode != dto.ItemCode && await _db.CcItemWeights.AnyAsync(x => x.IwItemCode == dto.ItemCode, ct))
            return Conflict(ApiResponse.Fail($"Item code '{dto.ItemCode}' already exists."));
        item.IwItemCode   = dto.ItemCode;
        item.IwItemDescr  = dto.ItemDescr;
        item.IwWeightCase = dto.WeightCase;
        item.IwWeightUnit = dto.WeightUnit;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CcItemWeight>.Ok(item, "Updated."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await _db.CcItemWeights.FirstOrDefaultAsync(x => x.IwId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Item weight {id} not found."));
        _db.CcItemWeights.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record ItemWeightDto(string ItemCode, string? ItemDescr, decimal WeightCase, decimal? WeightUnit, bool? IsActive);
