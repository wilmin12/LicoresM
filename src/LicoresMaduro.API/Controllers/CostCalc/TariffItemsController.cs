using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/tariff-items")]
[Authorize]
[Produces("application/json")]
public sealed class TariffItemsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public TariffItemsController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var q = _db.CcTariffItems.AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        var data = await q.OrderBy(x => x.TiHsCode).ToListAsync(ct);
        return Ok(ApiResponse<List<CcTariffItem>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _db.CcTariffItems.AsNoTracking().FirstOrDefaultAsync(x => x.TiId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Tariff item {id} not found."));
        return Ok(ApiResponse<CcTariffItem>.Ok(item));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] TariffItemDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        if (await _db.CcTariffItems.AnyAsync(x => x.TiHsCode == dto.HsCode, ct))
            return Conflict(ApiResponse.Fail($"HS Code '{dto.HsCode}' already exists."));

        var item = new CcTariffItem
        {
            TiHsCode      = dto.HsCode,
            TiDescription = dto.Description,
            TiDutyRate    = dto.DutyRate,
            TiEconRate    = dto.EconRate,
            TiObRate      = dto.ObRate,
            IsActive      = dto.IsActive ?? true,
            CreatedAt     = DateTime.UtcNow
        };
        _db.CcTariffItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.TiId }, ApiResponse<CcTariffItem>.Ok(item, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] TariffItemDto dto, CancellationToken ct)
    {
        var item = await _db.CcTariffItems.FirstOrDefaultAsync(x => x.TiId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Tariff item {id} not found."));

        if (item.TiHsCode != dto.HsCode && await _db.CcTariffItems.AnyAsync(x => x.TiHsCode == dto.HsCode, ct))
            return Conflict(ApiResponse.Fail($"HS Code '{dto.HsCode}' already exists."));

        item.TiHsCode      = dto.HsCode;
        item.TiDescription = dto.Description;
        item.TiDutyRate    = dto.DutyRate;
        item.TiEconRate    = dto.EconRate;
        item.TiObRate      = dto.ObRate;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CcTariffItem>.Ok(item, "Updated."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await _db.CcTariffItems.FirstOrDefaultAsync(x => x.TiId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Tariff item {id} not found."));
        _db.CcTariffItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record TariffItemDto(
    string  HsCode,
    string? Description,
    decimal DutyRate,
    decimal EconRate,
    decimal ObRate,
    bool?   IsActive
);
