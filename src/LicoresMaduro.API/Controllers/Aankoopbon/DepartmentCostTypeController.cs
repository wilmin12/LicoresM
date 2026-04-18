using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/department-cost-type")]
[Authorize]
[Produces("application/json")]
public sealed class DepartmentCostTypeController : ControllerBase
{
    private readonly ApplicationDbContext                  _db;
    private readonly IPermissionService                    _permissions;
    private readonly ILogger<DepartmentCostTypeController> _logger;

    public DepartmentCostTypeController(ApplicationDbContext db, IPermissionService permissions, ILogger<DepartmentCostTypeController> logger)
    { _db = db; _permissions = permissions; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 500, CancellationToken ct = default)
    {
        var q = _db.DepartmentCostTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.DctDepartment.Contains(search) || x.DctCostType.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.DctDepartment).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<DepartmentCostType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.DepartmentCostTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Department-costtype {id} not found.")) : Ok(ApiResponse<DepartmentCostType>.Ok(e));
    }

    [HttpGet("by-department/{department}")]
    public async Task<IActionResult> GetByDepartment(string department, CancellationToken ct)
    {
        var data = await _db.DepartmentCostTypes.AsNoTracking()
            .Where(x => x.IsActive && x.DctDepartment == department)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<DepartmentCostType>>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepartmentCostTypeDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_DEPT_COST_TYPE", "WRITE", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new DepartmentCostType { DctDepartment = dto.Department, DctCostType = dto.CostType, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.DepartmentCostTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DepartmentCostType created: '{Department}' -> '{CostType}'", dto.Department, dto.CostType);
        return CreatedAtAction(nameof(GetById), new { id = entity.DctId }, ApiResponse<DepartmentCostType>.Ok(entity, "Department-costtype link created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] DepartmentCostTypeDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_DEPT_COST_TYPE", "EDIT", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.DepartmentCostTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Department-costtype {id} not found."));
        entity.DctDepartment = dto.Department; entity.DctCostType = dto.CostType;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<DepartmentCostType>.Ok(entity, "Department-costtype link updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_DEPT_COST_TYPE", "EDIT", ct))
            return Forbid();
        var entity = await _db.DepartmentCostTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Department-costtype {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Department-costtype {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_DEPT_COST_TYPE", "DELETE", ct))
            return Forbid();
        var entity = await _db.DepartmentCostTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Department-costtype {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("DepartmentCostType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Department-costtype link deleted."));
    }
}

public sealed record DepartmentCostTypeDto(string Department, string CostType);
