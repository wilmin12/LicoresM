namespace LicoresMaduro.API.Models.Auth;

/// <summary>
/// Represents an application user stored in LM_Users.
/// </summary>
public sealed class LmUser
{
    public int       UserId       { get; set; }
    public string    Username     { get; set; } = string.Empty;
    public string    PasswordHash { get; set; } = string.Empty;
    public string    Email        { get; set; } = string.Empty;
    public string    FullName     { get; set; } = string.Empty;
    public bool      IsActive     { get; set; } = true;
    public DateTime  CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin    { get; set; }
    public int       RoleId       { get; set; }
    public string?   AvatarUrl    { get; set; }

    // Navigation
    public LmRole? Role { get; set; }
}
