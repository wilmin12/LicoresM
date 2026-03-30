using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/cat-add-specs")]
[Authorize]
[Produces("application/json")]
public sealed class CatAddSpecsController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly ILogger<CatAddSpecsController> _logger;

    public CatAddSpecsController(ApplicationDbContext db, ILogger<CatAddSpecsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.CatAddSpecs.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CatSel.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.CatSel).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<CatAddSpec>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.CatAddSpecs.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Cat add spec {id} not found.")) : Ok(ApiResponse<CatAddSpec>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CatSelDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new CatAddSpec { CatSel = dto.CatSel, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.CatAddSpecs.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.CasId }, ApiResponse<CatAddSpec>.Ok(entity, "Cat add spec created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CatSelDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.CatAddSpecs.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cat add spec {id} not found."));
        entity.CatSel = dto.CatSel;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CatAddSpec>.Ok(entity, "Cat add spec updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.CatAddSpecs.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cat add spec {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Cat add spec {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.CatAddSpecs.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cat add spec {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("CatAddSpec {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Cat add spec deleted."));
    }
}

/// <summary>Shared DTO for simple catalog tables that only have a CatSel field.</summary>
public sealed record CatSelDto(string CatSel);
