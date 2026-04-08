namespace LicoresMaduro.API.Models.Auth;

/// <summary>
/// Per-user permission override stored in LM_UserPermissions.
/// When a record exists for a user+submodule it takes precedence
/// over the role's permission for that submodule.
/// </summary>
public sealed class LmUserPermission
{
    public int  PermissionId { get; set; }   // UP_Id
    public int  UserId       { get; set; }
    public int  SubmoduleId  { get; set; }
    public bool CanAccess    { get; set; }
    public bool CanRead      { get; set; }
    public bool CanWrite     { get; set; }
    public bool CanEdit      { get; set; }
    public bool CanDelete    { get; set; }
    public bool CanApprove   { get; set; }

    // Navigation
    public LmUser?      User      { get; set; }
    public LmSubmodule? Submodule { get; set; }
}
