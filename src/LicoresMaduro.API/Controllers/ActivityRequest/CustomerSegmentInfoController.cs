using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/customer-segment-info")]
[Authorize]
[Produces("application/json")]
public sealed class CustomerSegmentInfoController : ControllerBase
{
    private readonly ApplicationDbContext                  _db;
    private readonly ILogger<CustomerSegmentInfoController> _logger;
    public CustomerSegmentInfoController(ApplicationDbContext db, ILogger<CustomerSegmentInfoController> logger) { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool includeInactive = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.CustomerSegmentInfos.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Code.Contains(search) || x.Description.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<CustomerSegmentInfo>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.CustomerSegmentInfos.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Customer segment info {id} not found.")) : Ok(ApiResponse<CustomerSegmentInfo>.Ok(e));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CodeDescriptionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new CustomerSegmentInfo { Code = dto.Code, Description = dto.Description, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.CustomerSegmentInfos.Add(entity); await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.CsiId }, ApiResponse<CustomerSegmentInfo>.Ok(entity, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CodeDescriptionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.CustomerSegmentInfos.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Customer segment info {id} not found."));
        entity.Code = dto.Code; entity.Description = dto.Description; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CustomerSegmentInfo>.Ok(entity, "Updated."));
    }

    [HttpPatch("{id:int}/toggle"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.CustomerSegmentInfos.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Customer segment info {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Customer segment info {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.CustomerSegmentInfos.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Customer segment info {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("CustomerSegmentInfo {Id} soft-deleted", id); return Ok(ApiResponse.Ok("Deleted."));
    }
}
