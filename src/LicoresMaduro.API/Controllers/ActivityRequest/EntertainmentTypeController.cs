using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/entertainment-type")]
[Authorize]
[Produces("application/json")]
public sealed class EntertainmentTypeController : ControllerBase
{
    private readonly ApplicationDbContext                _db;
    private readonly ILogger<EntertainmentTypeController> _logger;
    public EntertainmentTypeController(ApplicationDbContext db, ILogger<EntertainmentTypeController> logger) { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool includeInactive = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.EntertainmentTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Code.Contains(search) || x.Description.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<EntertainmentType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.EntertainmentTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Entertainment type {id} not found.")) : Ok(ApiResponse<EntertainmentType>.Ok(e));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CodeDescriptionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new EntertainmentType { Code = dto.Code, Description = dto.Description, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.EntertainmentTypes.Add(entity); await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.EtId }, ApiResponse<EntertainmentType>.Ok(entity, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CodeDescriptionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.EntertainmentTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Entertainment type {id} not found."));
        entity.Code = dto.Code; entity.Description = dto.Description; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<EntertainmentType>.Ok(entity, "Updated."));
    }

    [HttpPatch("{id:int}/toggle"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.EntertainmentTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Entertainment type {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Entertainment type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.EntertainmentTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Entertainment type {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("EntertainmentType {Id} soft-deleted", id); return Ok(ApiResponse.Ok("Deleted."));
    }
}
