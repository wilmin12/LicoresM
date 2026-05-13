using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/requestor-department")]
[Authorize]
[Produces("application/json")]
public sealed class RequestorDepartmentController : ControllerBase
{
    private readonly ApplicationDbContext                  _db;
    private readonly IPermissionService                    _permissions;
    private readonly ILogger<RequestorDepartmentController> _logger;

    public RequestorDepartmentController(ApplicationDbContext db, IPermissionService permissions, ILogger<RequestorDepartmentController> logger)
    { _db = db; _permissions = permissions; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 500, CancellationToken ct = default)
    {
        var q = _db.RequestorDepartments.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.RdRequestor.Contains(search) || x.RdDepartment.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RdRequestor).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<RequestorDepartment>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.RequestorDepartments.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Requestor-department {id} not found.")) : Ok(ApiResponse<RequestorDepartment>.Ok(e));
    }

    [HttpGet("by-requestor/{requestor}")]
    public async Task<IActionResult> GetByRequestor(string requestor, CancellationToken ct)
    {
        var data = await _db.RequestorDepartments.AsNoTracking()
            .Where(x => x.IsActive && x.RdRequestor == requestor)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<RequestorDepartment>>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RequestorDepartmentDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_REQUESTOR_DEPARTMENT", "WRITE", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new RequestorDepartment { RdRequestor = dto.Requestor, RdDepartment = dto.Department, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.RequestorDepartments.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("RequestorDepartment created: '{Requestor}' -> '{Department}'", dto.Requestor, dto.Department);
        return CreatedAtAction(nameof(GetById), new { id = entity.RdId }, ApiResponse<RequestorDepartment>.Ok(entity, "Requestor-department link created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RequestorDepartmentDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_REQUESTOR_DEPARTMENT", "EDIT", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.RequestorDepartments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor-department {id} not found."));
        entity.RdRequestor = dto.Requestor; entity.RdDepartment = dto.Department;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<RequestorDepartment>.Ok(entity, "Requestor-department link updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_REQUESTOR_DEPARTMENT", "EDIT", ct))
            return Forbid();
        var entity = await _db.RequestorDepartments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor-department {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Requestor-department {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_REQUESTOR_DEPARTMENT", "DELETE", ct))
            return Forbid();
        var entity = await _db.RequestorDepartments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor-department {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("RequestorDepartment {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Requestor-department link deleted."));
    }
}

public sealed record RequestorDepartmentDto(string Requestor, string Department);
