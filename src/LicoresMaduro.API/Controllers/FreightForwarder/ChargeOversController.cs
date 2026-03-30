using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/charge-overs")]
[Authorize]
[Produces("application/json")]
public sealed class ChargeOversController : ControllerBase
{
    private readonly ApplicationDbContext           _db;
    private readonly ILogger<ChargeOversController> _logger;

    public ChargeOversController(ApplicationDbContext db, ILogger<ChargeOversController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.ChargeOvers.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.CoCode.Contains(search) || x.CoDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.CoCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<ChargeOver>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ChargeOvers.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Charge over {id} not found.")) : Ok(ApiResponse<ChargeOver>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ChargeOverDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new ChargeOver { CoCode = dto.CoCode, CoDescription = dto.CoDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ChargeOvers.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ChargeOver '{Code}' created", dto.CoCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.CoId }, ApiResponse<ChargeOver>.Ok(entity, "Charge over created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ChargeOverDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.ChargeOvers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge over {id} not found."));
        entity.CoCode = dto.CoCode; entity.CoDescription = dto.CoDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ChargeOver>.Ok(entity, "Charge over updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.ChargeOvers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge over {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Charge over {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ChargeOvers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge over {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ChargeOver {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Charge over deleted."));
    }
}

public sealed record ChargeOverDto(string CoCode, string CoDescription);
