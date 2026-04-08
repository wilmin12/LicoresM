using System.ComponentModel.DataAnnotations;

namespace LicoresMaduro.API.DTOs.Auth;

// ── Request ────────────────────────────────────────────────────────────────────

/// <summary>Login request payload.</summary>
public sealed record LoginRequestDto(
    [Required][StringLength(50)] string Username,
    [Required][StringLength(128)] string Password
);

// ── Response ───────────────────────────────────────────────────────────────────

/// <summary>Successful login response with JWT token and user info.</summary>
public sealed record LoginResponseDto(
    string   Token,
    DateTime ExpiresAt,
    UserInfoDto User
);

/// <summary>Lightweight user info embedded in auth responses.</summary>
public sealed record UserInfoDto(
    int     UserId,
    string  Username,
    string  FullName,
    string  Email,
    string  RoleName,
    int     RoleId,
    string? AvatarUrl,
    IReadOnlyList<PermissionDto> Permissions
);

/// <summary>Serialized permission for a single submodule.</summary>
public sealed record PermissionDto(
    int    SubmoduleId,
    string SubmoduleName,
    string SubmoduleCode,
    string ModuleCode,
    bool   CanAccess,
    bool   CanRead,
    bool   CanWrite,
    bool   CanEdit,
    bool   CanDelete,
    bool   CanApprove
);
