using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/facilitators-info")]
[Authorize]
[Produces("application/json")]
public sealed class FacilitatorsInfoController : ControllerBase
{
    private readonly ApplicationDbContext               _db;
    private readonly ILogger<FacilitatorsInfoController> _logger;

    public FacilitatorsInfoController(ApplicationDbContext db, ILogger<FacilitatorsInfoController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.FacilitatorInfos.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Code.Contains(search) || x.Name.Contains(search) || (x.Email != null && x.Email.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<FacilitatorInfo>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.FacilitatorInfos.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Facilitator info {id} not found.")) : Ok(ApiResponse<FacilitatorInfo>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] FacilitatorInfoDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new FacilitatorInfo { Code = dto.Code, Name = dto.Name, Email = dto.Email, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.FacilitatorInfos.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("FacilitatorInfo '{Code}' created", dto.Code);
        return CreatedAtAction(nameof(GetById), new { id = entity.FiId }, ApiResponse<FacilitatorInfo>.Ok(entity, "Facilitator info created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] FacilitatorInfoDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.FacilitatorInfos.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Facilitator info {id} not found."));
        entity.Code = dto.Code; entity.Name = dto.Name; entity.Email = dto.Email;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FacilitatorInfo>.Ok(entity, "Facilitator info updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.FacilitatorInfos.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Facilitator info {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Facilitator info {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.FacilitatorInfos.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Facilitator info {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("FacilitatorInfo {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Facilitator info deleted."));
    }
}

public sealed record FacilitatorInfoDto(string Code, string Name, string? Email);
