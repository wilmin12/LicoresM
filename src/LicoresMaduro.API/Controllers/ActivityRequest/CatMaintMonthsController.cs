using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/cat-maint-months")]
[Authorize]
[Produces("application/json")]
public sealed class CatMaintMonthsController : ControllerBase
{
    private readonly ApplicationDbContext               _db;
    private readonly ILogger<CatMaintMonthsController> _logger;
    public CatMaintMonthsController(ApplicationDbContext db, ILogger<CatMaintMonthsController> logger) { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool includeInactive = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.CatMaintMonths.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CatSel.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.CatSel).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<CatMaintMonth>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.CatMaintMonths.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Cat maint month {id} not found.")) : Ok(ApiResponse<CatMaintMonth>.Ok(e));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CatSelDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new CatMaintMonth { CatSel = dto.CatSel, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.CatMaintMonths.Add(entity); await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.CmmId }, ApiResponse<CatMaintMonth>.Ok(entity, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CatSelDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.CatMaintMonths.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cat maint month {id} not found."));
        entity.CatSel = dto.CatSel; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CatMaintMonth>.Ok(entity, "Updated."));
    }

    [HttpPatch("{id:int}/toggle"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.CatMaintMonths.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cat maint month {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Cat maint month {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.CatMaintMonths.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Cat maint month {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("CatMaintMonth {Id} soft-deleted", id); return Ok(ApiResponse.Ok("Deleted."));
    }
}
