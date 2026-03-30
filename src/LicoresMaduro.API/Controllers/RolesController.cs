using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers;

// ── Request DTOs (inline for single-file simplicity) ──────────────────────────

public sealed record CreateRoleDto(
    string RoleName,
    string Description
);

public sealed record UpdateRoleDto(
    string RoleName,
    string Description,
    bool   IsActive
);

public sealed record PermissionAssignmentDto(
    string SubmoduleCode,
    bool   CanAccess,
    bool   CanRead,
    bool   CanWrite,
    bool   CanEdit,
    bool   CanDelete
);

// ── Controller ─────────────────────────────────────────────────────────────────

/// <summary>Role management and permission assignment.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Produces("application/json")]
public sealed class RolesController : ControllerBase
{
    private readonly ApplicationDbContext    _db;
    private readonly ILogger<RolesController> _logger;

    public RolesController(ApplicationDbContext db, ILogger<RolesController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── GET /api/roles ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var roles = await _db.LmRoles
            .OrderBy(r => r.RoleName)
            .ToListAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<LmRole>>.Ok(roles));
    }

    // ── GET /api/roles/{id} ────────────────────────────────────────────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var role = await _db.LmRoles.FindAsync([id], ct);
        if (role is null)
            return NotFound(ApiResponse.Fail($"Role {id} not found."));
        return Ok(ApiResponse<LmRole>.Ok(role));
    }

    // ── POST /api/roles ────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto, CancellationToken ct)
    {
        if (await _db.LmRoles.AnyAsync(r => r.RoleName == dto.RoleName, ct))
            return Conflict(ApiResponse.Fail($"Role '{dto.RoleName}' already exists."));

        var role = new LmRole { RoleName = dto.RoleName, Description = dto.Description };
        _db.LmRoles.Add(role);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = role.RoleId },
            ApiResponse<LmRole>.Ok(role, "Role created."));
    }

    // ── PUT /api/roles/{id} ────────────────────────────────────────────────────

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleDto dto, CancellationToken ct)
    {
        var role = await _db.LmRoles.FindAsync([id], ct);
        if (role is null) return NotFound(ApiResponse.Fail($"Role {id} not found."));

        role.RoleName    = dto.RoleName;
        role.Description = dto.Description;
        role.IsActive    = dto.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LmRole>.Ok(role, "Role updated."));
    }

    // ── PATCH /api/roles/{id}/toggle-status ───────────────────────────────────

    [HttpPatch("{id:int}/toggle-status")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var role = await _db.LmRoles.FindAsync([id], ct);
        if (role is null) return NotFound(ApiResponse.Fail($"Role {id} not found."));

        role.IsActive = !role.IsActive;
        await _db.SaveChangesAsync(ct);

        var status = role.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("Role {RoleId} {Status} by {User}", id, status, User.Identity?.Name);
        return Ok(ApiResponse<LmRole>.Ok(role, $"Role {status}."));
    }

    // ── DELETE /api/roles/{id} ─────────────────────────────────────────────────

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var role = await _db.LmRoles
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.RoleId == id, ct);

        if (role is null) return NotFound(ApiResponse.Fail($"Role {id} not found."));

        if (role.Users.Any())
            return Conflict(ApiResponse.Fail(
                $"Cannot delete '{role.RoleName}': {role.Users.Count} user(s) are assigned to it. Deactivate it instead."));

        var perms = await _db.LmRolePermissions.Where(p => p.RoleId == id).ToListAsync(ct);
        _db.LmRolePermissions.RemoveRange(perms);
        _db.LmRoles.Remove(role);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Role {RoleId} ({Name}) deleted by {User}", id, role.RoleName, User.Identity?.Name);
        return Ok(ApiResponse.Ok($"Role '{role.RoleName}' deleted successfully."));
    }

    // ── GET /api/roles/{id}/permissions ────────────────────────────────────────

    [HttpGet("{id:int}/permissions")]
    public async Task<IActionResult> GetPermissions(int id, CancellationToken ct)
    {
        var perms = await _db.LmRolePermissions
            .Where(p => p.RoleId == id)
            .Select(p => new
            {
                SubmoduleCode = p.Submodule!.SubmoduleCode,
                p.CanAccess,
                p.CanRead,
                p.CanWrite,
                p.CanEdit,
                p.CanDelete
            })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(perms));
    }

    // ── PUT /api/roles/{id}/permissions ────────────────────────────────────────

    /// <summary>Bulk-upsert permissions for a role.</summary>
    [HttpPut("{id:int}/permissions")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetPermissions(
        int id,
        [FromBody] IReadOnlyList<PermissionAssignmentDto> dtos,
        CancellationToken ct)
    {
        if (!await _db.LmRoles.AnyAsync(r => r.RoleId == id, ct))
            return NotFound(ApiResponse.Fail($"Role {id} not found."));

        // Resolve all submodule codes to IDs in one query
        var codes = dtos.Select(d => d.SubmoduleCode).Distinct().ToList();
        var submoduleMap = await _db.LmSubmodules
            .Where(s => codes.Contains(s.SubmoduleCode))
            .ToDictionaryAsync(s => s.SubmoduleCode, s => s.SubmoduleId, ct);

        // Load existing permissions for this role in one query
        var existingPerms = await _db.LmRolePermissions
            .Where(p => p.RoleId == id)
            .ToListAsync(ct);
        var existingMap = existingPerms.ToDictionary(p => p.SubmoduleId);

        foreach (var dto in dtos)
        {
            if (!submoduleMap.TryGetValue(dto.SubmoduleCode, out var subId))
            {
                _logger.LogWarning("SetPermissions: unknown SubmoduleCode '{Code}' — skipped.", dto.SubmoduleCode);
                continue;
            }

            if (existingMap.TryGetValue(subId, out var existing))
            {
                existing.CanAccess = dto.CanAccess;
                existing.CanRead   = dto.CanRead;
                existing.CanWrite  = dto.CanWrite;
                existing.CanEdit   = dto.CanEdit;
                existing.CanDelete = dto.CanDelete;
            }
            else
            {
                _db.LmRolePermissions.Add(new LmRolePermission
                {
                    RoleId      = id,
                    SubmoduleId = subId,
                    CanAccess   = dto.CanAccess,
                    CanRead     = dto.CanRead,
                    CanWrite    = dto.CanWrite,
                    CanEdit     = dto.CanEdit,
                    CanDelete   = dto.CanDelete
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Permissions updated for role {RoleId} by {Caller}", id, User.Identity?.Name);
        return Ok(ApiResponse.Ok("Permissions saved successfully."));
    }
}
