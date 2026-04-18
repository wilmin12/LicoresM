using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/vendor-department")]
[Authorize]
[Produces("application/json")]
public sealed class VendorDepartmentController : ControllerBase
{
    private readonly ApplicationDbContext               _db;
    private readonly IPermissionService                 _permissions;
    private readonly ILogger<VendorDepartmentController> _logger;

    public VendorDepartmentController(ApplicationDbContext db, IPermissionService permissions, ILogger<VendorDepartmentController> logger)
    { _db = db; _permissions = permissions; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 500, CancellationToken ct = default)
    {
        var q = _db.VendorDepartments.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.VdVendor.Contains(search) || x.VdDepartment.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.VdVendor).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<VendorDepartment>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.VendorDepartments.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Vendor-department {id} not found.")) : Ok(ApiResponse<VendorDepartment>.Ok(e));
    }

    [HttpGet("by-vendor/{vendor}")]
    public async Task<IActionResult> GetByVendor(string vendor, CancellationToken ct)
    {
        var data = await _db.VendorDepartments.AsNoTracking()
            .Where(x => x.IsActive && x.VdVendor == vendor)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<VendorDepartment>>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VendorDepartmentDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_VENDOR_DEPARTMENT", "WRITE", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new VendorDepartment { VdVendor = dto.Vendor, VdDepartment = dto.Department, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.VendorDepartments.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("VendorDepartment created: '{Vendor}' -> '{Department}'", dto.Vendor, dto.Department);
        return CreatedAtAction(nameof(GetById), new { id = entity.VdId }, ApiResponse<VendorDepartment>.Ok(entity, "Vendor-department link created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VendorDepartmentDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_VENDOR_DEPARTMENT", "EDIT", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.VendorDepartments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vendor-department {id} not found."));
        entity.VdVendor = dto.Vendor; entity.VdDepartment = dto.Department;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<VendorDepartment>.Ok(entity, "Vendor-department link updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_VENDOR_DEPARTMENT", "EDIT", ct))
            return Forbid();
        var entity = await _db.VendorDepartments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vendor-department {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Vendor-department {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_VENDOR_DEPARTMENT", "DELETE", ct))
            return Forbid();
        var entity = await _db.VendorDepartments.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vendor-department {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("VendorDepartment {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Vendor-department link deleted."));
    }
}

public sealed record VendorDepartmentDto(string Vendor, string Department);
