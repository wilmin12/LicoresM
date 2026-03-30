namespace LicoresMaduro.API.Models.Auth;

/// <summary>
/// Represents a submodule (catalog table) stored in LM_Submodules.
/// Each submodule maps to one web-managed database table.
/// </summary>
public sealed class LmSubmodule
{
    public int     SubmoduleId   { get; set; }
    public int     ModuleId      { get; set; }
    public string  SubmoduleName { get; set; } = string.Empty;
    public string  SubmoduleCode { get; set; } = string.Empty;
    public string? TableName     { get; set; }
    public int     DisplayOrder  { get; set; }
    public bool    IsActive      { get; set; } = true;

    // Navigation
    public LmModule?                     Module      { get; set; }
    public ICollection<LmRolePermission> Permissions { get; set; } = [];
}
