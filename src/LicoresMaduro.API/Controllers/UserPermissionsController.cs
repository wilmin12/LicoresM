using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers;

public sealed record UserPermissionAssignmentDto(
    string SubmoduleCode,
    bool   CanAccess,
    bool   CanRead,
    bool   CanWrite,
    bool   CanEdit,
    bool   CanDelete,
    bool   CanApprove
);

/// <summary>Manage per-user permission overrides.</summary>
[ApiController]
[Route("api/users/{userId:int}/permissions")]
[Authorize(Roles = "SuperAdmin,Admin")]
[Produces("application/json")]
public sealed class UserPermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UserPermissionsController> _logger;

    public UserPermissionsController(ApplicationDbContext db, ILogger<UserPermissionsController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── GET /api/users/{userId}/permissions ────────────────────────────────────
    /// <summary>Returns the user's permission overrides (empty list = no overrides set).</summary>
    [HttpGet]
    public async Task<IActionResult> GetUserPermissions(int userId, CancellationToken ct)
    {
        if (!await _db.LmUsers.AnyAsync(u => u.UserId == userId, ct))
            return NotFound(ApiResponse.Fail($"User {userId} not found."));

        var overrides = await _db.LmUserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                SubmoduleCode = p.Submodule!.SubmoduleCode,
                p.CanAccess,
                p.CanRead,
                p.CanWrite,
                p.CanEdit,
                p.CanDelete,
                p.CanApprove
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(overrides));
    }

    // ── PUT /api/users/{userId}/permissions ────────────────────────────────────
    /// <summary>
    /// Bulk-upsert user permission overrides.
    /// Send an empty array to remove all overrides for this user.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetUserPermissions(
        int userId,
        [FromBody] IReadOnlyList<UserPermissionAssignmentDto> dtos,
        CancellationToken ct)
    {
        if (!await _db.LmUsers.AnyAsync(u => u.UserId == userId, ct))
            return NotFound(ApiResponse.Fail($"User {userId} not found."));

        // Resolve submodule codes → IDs in one query
        var codes = dtos.Select(d => d.SubmoduleCode).Distinct().ToList();
        var submoduleMap = await _db.LmSubmodules
            .Where(s => codes.Contains(s.SubmoduleCode))
            .ToDictionaryAsync(s => s.SubmoduleCode, s => s.SubmoduleId, ct);

        // Load existing overrides for this user
        var existing = await _db.LmUserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);
        var existingMap = existing.ToDictionary(p => p.SubmoduleId);

        // Remove overrides that are no longer in the payload
        var incomingSubIds = dtos
            .Where(d => submoduleMap.ContainsKey(d.SubmoduleCode))
            .Select(d => submoduleMap[d.SubmoduleCode])
            .ToHashSet();

        var toRemove = existing.Where(p => !incomingSubIds.Contains(p.SubmoduleId)).ToList();
        _db.LmUserPermissions.RemoveRange(toRemove);

        // Upsert
        foreach (var dto in dtos)
        {
            if (!submoduleMap.TryGetValue(dto.SubmoduleCode, out var subId))
            {
                _logger.LogWarning("SetUserPermissions: unknown SubmoduleCode '{Code}' — skipped.", dto.SubmoduleCode);
                continue;
            }

            if (existingMap.TryGetValue(subId, out var ov))
            {
                ov.CanAccess  = dto.CanAccess;
                ov.CanRead    = dto.CanRead;
                ov.CanWrite   = dto.CanWrite;
                ov.CanEdit    = dto.CanEdit;
                ov.CanDelete  = dto.CanDelete;
                ov.CanApprove = dto.CanApprove;
            }
            else
            {
                _db.LmUserPermissions.Add(new LmUserPermission
                {
                    UserId      = userId,
                    SubmoduleId = subId,
                    CanAccess   = dto.CanAccess,
                    CanRead     = dto.CanRead,
                    CanWrite    = dto.CanWrite,
                    CanEdit     = dto.CanEdit,
                    CanDelete   = dto.CanDelete,
                    CanApprove  = dto.CanApprove
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("User {UserId} permissions updated by {Caller}", userId, User.Identity?.Name);
        return Ok(ApiResponse.Ok("User permissions saved successfully."));
    }

    // ── DELETE /api/users/{userId}/permissions ─────────────────────────────────
    /// <summary>Remove all permission overrides for a user (reverts to role defaults).</summary>
    [HttpDelete]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ClearUserPermissions(int userId, CancellationToken ct)
    {
        var overrides = await _db.LmUserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        _db.LmUserPermissions.RemoveRange(overrides);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("All permission overrides cleared for user {UserId} by {Caller}", userId, User.Identity?.Name);
        return Ok(ApiResponse.Ok("Permission overrides cleared. User now inherits role permissions."));
    }
}
