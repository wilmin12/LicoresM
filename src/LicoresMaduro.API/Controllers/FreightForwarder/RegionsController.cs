using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/regions")]
[Authorize]
[Produces("application/json")]
public sealed class RegionsController : ControllerBase
{
    private readonly ApplicationDbContext      _db;
    private readonly ILogger<RegionsController> _logger;

    public RegionsController(ApplicationDbContext db, ILogger<RegionsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 200, CancellationToken ct = default)
    {
        var q = _db.Regions.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.RegCode.Contains(search) || x.RegName.Contains(search) ||
                             (x.RegCountry != null && x.RegCountry.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RegCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<Region>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Regions.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Region {id} not found."))
                         : Ok(ApiResponse<Region>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] RegionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new Region
        {
            RegCode    = dto.RegCode.ToUpperInvariant(),
            RegName    = dto.RegName,
            RegCountry = dto.RegCountry,
            IsActive   = true,
            CreatedAt  = DateTime.UtcNow
        };
        _db.Regions.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Region '{Code}' created", dto.RegCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.RegId }, ApiResponse<Region>.Ok(entity, "Region created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] RegionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.Regions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region {id} not found."));
        entity.RegCode    = dto.RegCode.ToUpperInvariant();
        entity.RegName    = dto.RegName;
        entity.RegCountry = dto.RegCountry;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Region>.Ok(entity, "Region updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.Regions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Region {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Regions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Region {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Region {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Region deleted."));
    }
}

public sealed record RegionDto(
    [Required][MaxLength(50)] string RegCode,
    [Required][MaxLength(50)] string RegName,
    [MaxLength(100)] string? RegCountry
);
