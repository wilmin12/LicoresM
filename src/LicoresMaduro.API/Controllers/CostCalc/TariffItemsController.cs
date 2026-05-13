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
        var data = await q.OrderBy(x => x.Hs6Cod).ToListAsync(ct);
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

        if (await _db.CcTariffItems.AnyAsync(x => x.Hs6Cod == dto.Hs6Cod, ct))
            return Conflict(ApiResponse.Fail($"HS Code '{dto.Hs6Cod}' already exists."));

        var item = new CcTariffItem
        {
            Hs6Cod    = dto.Hs6Cod,
            TarPr1    = dto.TarPr1,
            TarDsc    = dto.TarDsc,
            UomCod1   = dto.UomCod1,
            UomCod2   = dto.UomCod2,
            TarT01    = dto.TarT01,
            TarT02    = dto.TarT02,
            TarT04    = dto.TarT04,
            TarT05    = dto.TarT05,
            TarT06    = dto.TarT06,
            TarT07    = dto.TarT07,
            TarT08    = dto.TarT08,
            TarT09    = dto.TarT09,
            TarT10    = dto.TarT10,
            TarT12    = dto.TarT12,
            IsActive  = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow
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

        if (item.Hs6Cod != dto.Hs6Cod && await _db.CcTariffItems.AnyAsync(x => x.Hs6Cod == dto.Hs6Cod, ct))
            return Conflict(ApiResponse.Fail($"HS Code '{dto.Hs6Cod}' already exists."));

        item.Hs6Cod   = dto.Hs6Cod;
        item.TarPr1   = dto.TarPr1;
        item.TarDsc   = dto.TarDsc;
        item.UomCod1  = dto.UomCod1;
        item.UomCod2  = dto.UomCod2;
        item.TarT01   = dto.TarT01;
        item.TarT02   = dto.TarT02;
        item.TarT04   = dto.TarT04;
        item.TarT05   = dto.TarT05;
        item.TarT06   = dto.TarT06;
        item.TarT07   = dto.TarT07;
        item.TarT08   = dto.TarT08;
        item.TarT09   = dto.TarT09;
        item.TarT10   = dto.TarT10;
        item.TarT12   = dto.TarT12;
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
    string  Hs6Cod,
    string? TarPr1,
    string? TarDsc,
    string? UomCod1,
    string? UomCod2,
    decimal? TarT01,
    decimal? TarT02,
    decimal? TarT04,
    decimal? TarT05,
    decimal? TarT06,
    decimal? TarT07,
    decimal? TarT08,
    decimal? TarT09,
    decimal? TarT10,
    decimal? TarT12,
    bool?   IsActive
);
