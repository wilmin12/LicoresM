using System.Security.Claims;
using LicoresMaduro.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Services;

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int roleId, string submoduleCode, string permissionType, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string submoduleCode, string permissionType, CancellationToken ct = default);
    Task<PermissionResult?> GetPermissionAsync(int roleId, string submoduleCode, CancellationToken ct = default);
}

/// <summary>Resolved permission state for a role/submodule combination.</summary>
public sealed record PermissionResult(
    bool CanAccess,
    bool CanRead,
    bool CanWrite,
    bool CanEdit,
    bool CanDelete
);

// ── Implementation ─────────────────────────────────────────────────────────────

public sealed class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext        _db;
    private readonly ILogger<PermissionService>  _logger;

    public PermissionService(ApplicationDbContext db, ILogger<PermissionService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(
        int    roleId,
        string submoduleCode,
        string permissionType,
        CancellationToken ct = default)
    {
        var perm = await GetPermissionAsync(roleId, submoduleCode, ct);
        if (perm is null) return false;

        return permissionType.ToUpperInvariant() switch
        {
            "ACCESS" => perm.CanAccess,
            "READ"   => perm.CanRead,
            "WRITE"  => perm.CanWrite,
            "EDIT"   => perm.CanEdit,
            "DELETE" => perm.CanDelete,
            _        => false
        };
    }

    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal user,
        string          submoduleCode,
        string          permissionType,
        CancellationToken ct = default)
    {
        // SuperAdmin bypasses all permission checks
        if (user.IsInRole("SuperAdmin")) return true;

        var roleIdClaim = user.FindFirstValue("roleId");
        if (!int.TryParse(roleIdClaim, out var roleId)) return false;

        return await HasPermissionAsync(roleId, submoduleCode, permissionType, ct);
    }

    /// <inheritdoc/>
    public async Task<PermissionResult?> GetPermissionAsync(
        int    roleId,
        string submoduleCode,
        CancellationToken ct = default)
    {
        var perm = await _db.LmRolePermissions
            .Include(p => p.Submodule)
            .FirstOrDefaultAsync(
                p => p.RoleId == roleId
                  && p.Submodule!.SubmoduleCode == submoduleCode
                  && p.Submodule.IsActive,
                ct);

        if (perm is null)
        {
            _logger.LogDebug("No permission found for roleId={RoleId}, submodule={Code}", roleId, submoduleCode);
            return null;
        }

        return new PermissionResult(
            CanAccess: perm.CanAccess,
            CanRead:   perm.CanRead,
            CanWrite:  perm.CanWrite,
            CanEdit:   perm.CanEdit,
            CanDelete: perm.CanDelete
        );
    }
}
