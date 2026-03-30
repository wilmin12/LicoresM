using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/activity-types")]
[Authorize]
[Produces("application/json")]
public sealed class ActivityTypesController : ControllerBase
{
    private readonly ApplicationDbContext             _db;
    private readonly ILogger<ActivityTypesController> _logger;

    public ActivityTypesController(ApplicationDbContext db, ILogger<ActivityTypesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.ActivityTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Code.Contains(search) || x.Description.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<ActivityType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ActivityTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Activity type {id} not found.")) : Ok(ApiResponse<ActivityType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ActivityTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new ActivityType { Code = dto.Code, Description = dto.Description, ActivityRelated = dto.ActivityRelated, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ActivityTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ActivityType '{Code}' created", dto.Code);
        return CreatedAtAction(nameof(GetById), new { id = entity.AtId }, ApiResponse<ActivityType>.Ok(entity, "Activity type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ActivityTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.ActivityTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Activity type {id} not found."));
        entity.Code = dto.Code; entity.Description = dto.Description; entity.ActivityRelated = dto.ActivityRelated;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ActivityType>.Ok(entity, "Activity type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.ActivityTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Activity type {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Activity type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ActivityTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Activity type {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ActivityType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Activity type deleted."));
    }
}

public sealed record ActivityTypeDto(string Code, string Description, bool ActivityRelated);
