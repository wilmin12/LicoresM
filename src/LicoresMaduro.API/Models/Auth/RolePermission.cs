namespace LicoresMaduro.API.Models.Auth;

/// <summary>
/// Maps a role to a submodule with granular CRUD permissions (LM_RolePermissions).
/// </summary>
public sealed class LmRolePermission
{
    public int  PermissionId { get; set; }
    public int  RoleId       { get; set; }
    public int  SubmoduleId  { get; set; }
    public bool CanAccess    { get; set; }
    public bool CanRead      { get; set; }
    public bool CanWrite     { get; set; }
    public bool CanEdit      { get; set; }
    public bool CanDelete    { get; set; }

    // Navigation
    public LmRole?      Role      { get; set; }
    public LmSubmodule? Submodule { get; set; }
}
