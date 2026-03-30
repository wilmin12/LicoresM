namespace LicoresMaduro.API.Models.Auth;

/// <summary>
/// Represents a security role stored in LM_Roles.
/// </summary>
public sealed class LmRole
{
    public int    RoleId      { get; set; }
    public string RoleName    { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool   IsActive    { get; set; } = true;

    // Navigation
    public ICollection<LmUser>           Users       { get; set; } = [];
    public ICollection<LmRolePermission> Permissions { get; set; } = [];
}
