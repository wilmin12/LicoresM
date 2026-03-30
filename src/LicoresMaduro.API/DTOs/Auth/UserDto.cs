using System.ComponentModel.DataAnnotations;

namespace LicoresMaduro.API.DTOs.Auth;

// ── Create ─────────────────────────────────────────────────────────────────────

/// <summary>Payload to create a new user.</summary>
public sealed record CreateUserDto(
    [Required][StringLength(50)]  string Username,
    [Required][StringLength(128)] string Password,
    [Required][EmailAddress][StringLength(100)] string Email,
    [Required][StringLength(100)] string FullName,
    [Required][Range(1, int.MaxValue)] int RoleId
);

// ── Update ─────────────────────────────────────────────────────────────────────

/// <summary>Payload to update an existing user (password excluded).</summary>
public sealed record UpdateUserDto(
    [Required][StringLength(50)]  string Username,
    [Required][EmailAddress][StringLength(100)] string Email,
    [Required][StringLength(100)] string FullName,
    [Required][Range(1, int.MaxValue)] int RoleId,
    bool IsActive
);

// ── Change Password ────────────────────────────────────────────────────────────

/// <summary>Payload for password change operations.</summary>
public sealed record ChangePasswordDto(
    [Required][StringLength(128)] string CurrentPassword,
    [Required][StringLength(128)] string NewPassword,
    [Required][StringLength(128)] string ConfirmNewPassword
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (NewPassword != ConfirmNewPassword)
            yield return new ValidationResult(
                "New password and confirmation do not match.",
                [nameof(ConfirmNewPassword)]);
    }
}

// ── Response ───────────────────────────────────────────────────────────────────

/// <summary>User record returned by GET endpoints.</summary>
public sealed record UserResponseDto(
    int       UserId,
    string    Username,
    string    Email,
    string    FullName,
    bool      IsActive,
    DateTime  CreatedAt,
    DateTime? LastLogin,
    int       RoleId,
    string    RoleName,
    string?   AvatarUrl
);
