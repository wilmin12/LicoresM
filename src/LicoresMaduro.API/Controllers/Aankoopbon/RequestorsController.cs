using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/requestors")]
[Authorize]
[Produces("application/json")]
public sealed class RequestorsController : ControllerBase
{
    private readonly ApplicationDbContext           _db;
    private readonly ILogger<RequestorsController> _logger;

    public RequestorsController(ApplicationDbContext db, ILogger<RequestorsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.Requestors.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.ReqName.Contains(search) || (x.ReqEmail != null && x.ReqEmail.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.ReqName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<Requestor>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Requestors.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Requestor {id} not found.")) : Ok(ApiResponse<Requestor>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] RequestorDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new Requestor { ReqName = dto.ReqName, ReqEmail = dto.ReqEmail, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Requestors.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Requestor '{Name}' created", dto.ReqName);
        return CreatedAtAction(nameof(GetById), new { id = entity.ReqId }, ApiResponse<Requestor>.Ok(entity, "Requestor created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] RequestorDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.Requestors.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor {id} not found."));
        entity.ReqName = dto.ReqName; entity.ReqEmail = dto.ReqEmail;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Requestor>.Ok(entity, "Requestor updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.Requestors.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Requestor {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Requestors.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Requestor {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Requestor deleted."));
    }
}

public sealed record RequestorDto(string ReqName, string? ReqEmail);
