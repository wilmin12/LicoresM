using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/charge-actions")]
[Authorize]
[Produces("application/json")]
public sealed class ChargeActionsController : ControllerBase
{
    private readonly ApplicationDbContext             _db;
    private readonly ILogger<ChargeActionsController> _logger;

    public ChargeActionsController(ApplicationDbContext db, ILogger<ChargeActionsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.ChargeActions.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.CaCode.Contains(search) || x.CaDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.CaCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<ChargeAction>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ChargeActions.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Charge action {id} not found.")) : Ok(ApiResponse<ChargeAction>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ChargeActionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new ChargeAction { CaCode = dto.CaCode, CaDescription = dto.CaDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ChargeActions.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ChargeAction '{Code}' created", dto.CaCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.CaId }, ApiResponse<ChargeAction>.Ok(entity, "Charge action created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ChargeActionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.ChargeActions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge action {id} not found."));
        entity.CaCode = dto.CaCode; entity.CaDescription = dto.CaDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ChargeAction>.Ok(entity, "Charge action updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.ChargeActions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge action {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Charge action {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ChargeActions.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge action {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ChargeAction {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Charge action deleted."));
    }
}

public sealed record ChargeActionDto(string CaCode, string CaDescription);
