using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/departments")]
[Authorize]
[Produces("application/json")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext           _db;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(ApplicationDbContext db, ILogger<DepartmentsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.Departments.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.DpName.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.DpName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<Department>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Departments.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Department {id} not found.")) : Ok(ApiResponse<Department>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] DepartmentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new Department { DpName = dto.DpName, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Departments.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Department '{Name}' created", dto.DpName);
        return CreatedAtAction(nameof(GetById), new { id = entity.DpId }, ApiResponse<Department>.Ok(entity, "Department created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] DepartmentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.Departments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Department {id} not found."));
        entity.DpName = dto.DpName; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Department>.Ok(entity, "Department updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.Departments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Department {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Department {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Departments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Department {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Department {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Department deleted."));
    }
}

public sealed record DepartmentDto(string DpName);
