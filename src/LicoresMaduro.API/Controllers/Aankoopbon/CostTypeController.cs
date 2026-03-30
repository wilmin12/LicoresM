using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/cost-types")]
[Authorize]
[Produces("application/json")]
public sealed class CostTypeController : ControllerBase
{
    private readonly ApplicationDbContext         _db;
    private readonly ILogger<CostTypeController> _logger;

    public CostTypeController(ApplicationDbContext db, ILogger<CostTypeController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.CostTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.TcName.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.TcName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<CostType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.CostTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Cost type {id} not found.")) : Ok(ApiResponse<CostType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CostTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new CostType { TcName = dto.TcName, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.CostTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CostType '{Name}' created", dto.TcName);
        return CreatedAtAction(nameof(GetById), new { id = entity.CtId }, ApiResponse<CostType>.Ok(entity, "Cost type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CostTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.CostTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cost type {id} not found."));
        entity.TcName = dto.TcName; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CostType>.Ok(entity, "Cost type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.CostTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cost type {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Cost type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.CostTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cost type {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("CostType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Cost type deleted."));
    }
}

public sealed record CostTypeDto(string TcName);
