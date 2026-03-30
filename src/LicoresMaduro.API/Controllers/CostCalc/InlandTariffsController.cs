using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/inland-tariffs")]
[Authorize]
[Produces("application/json")]
public sealed class InlandTariffsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public InlandTariffsController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var q = _db.CcInlandTariffs.AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        var data = await q.OrderBy(x => x.ItHsCode).ToListAsync(ct);
        return Ok(ApiResponse<List<CcInlandTariff>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _db.CcInlandTariffs.AsNoTracking().FirstOrDefaultAsync(x => x.ItId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Inland tariff {id} not found."));
        return Ok(ApiResponse<CcInlandTariff>.Ok(item));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] InlandTariffDto dto, CancellationToken ct)
    {
        if (await _db.CcInlandTariffs.AnyAsync(x => x.ItHsCode == dto.HsCode, ct))
            return Conflict(ApiResponse.Fail($"HS Code '{dto.HsCode}' already exists."));
        var item = new CcInlandTariff
        {
            ItHsCode      = dto.HsCode,
            ItDescription = dto.Description,
            ItRate        = dto.Rate,
            IsActive      = dto.IsActive ?? true,
            CreatedAt     = DateTime.UtcNow
        };
        _db.CcInlandTariffs.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.ItId }, ApiResponse<CcInlandTariff>.Ok(item, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] InlandTariffDto dto, CancellationToken ct)
    {
        var item = await _db.CcInlandTariffs.FirstOrDefaultAsync(x => x.ItId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Inland tariff {id} not found."));
        if (item.ItHsCode != dto.HsCode && await _db.CcInlandTariffs.AnyAsync(x => x.ItHsCode == dto.HsCode, ct))
            return Conflict(ApiResponse.Fail($"HS Code '{dto.HsCode}' already exists."));
        item.ItHsCode      = dto.HsCode;
        item.ItDescription = dto.Description;
        item.ItRate        = dto.Rate;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CcInlandTariff>.Ok(item, "Updated."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await _db.CcInlandTariffs.FirstOrDefaultAsync(x => x.ItId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Inland tariff {id} not found."));
        _db.CcInlandTariffs.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record InlandTariffDto(string HsCode, string? Description, decimal Rate, bool? IsActive);
