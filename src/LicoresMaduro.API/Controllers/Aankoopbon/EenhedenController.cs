using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/eenheden")]
[Authorize]
[Produces("application/json")]
public sealed class EenhedenController : ControllerBase
{
    private readonly ApplicationDbContext         _db;
    private readonly IPermissionService           _permissions;
    private readonly ILogger<EenhedenController> _logger;

    public EenhedenController(ApplicationDbContext db, IPermissionService permissions, ILogger<EenhedenController> logger)
    { _db = db; _permissions = permissions; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.Eenheden.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.UnitCode.Contains(search) || x.Omschrijving.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.UnitCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<Eenheid>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Eenheden.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Eenheid {id} not found.")) : Ok(ApiResponse<Eenheid>.Ok(e));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EenheidDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_EENHEDEN", "WRITE", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new Eenheid { UnitCode = dto.UnitCode, Omschrijving = dto.Omschrijving, OmrekenFaktor = dto.OmrekenFaktor, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Eenheden.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Eenheid '{Code}' created", dto.UnitCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.EeId }, ApiResponse<Eenheid>.Ok(entity, "Eenheid created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EenheidDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_EENHEDEN", "EDIT", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.Eenheden.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Eenheid {id} not found."));
        entity.UnitCode = dto.UnitCode; entity.Omschrijving = dto.Omschrijving; entity.OmrekenFaktor = dto.OmrekenFaktor;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Eenheid>.Ok(entity, "Eenheid updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_EENHEDEN", "EDIT", ct))
            return Forbid();
        var entity = await _db.Eenheden.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Eenheid {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Eenheid {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_EENHEDEN", "DELETE", ct))
            return Forbid();
        var entity = await _db.Eenheden.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Eenheid {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Eenheid {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Eenheid deleted."));
    }
}

public sealed record EenheidDto(string UnitCode, string Omschrijving, double? OmrekenFaktor);
