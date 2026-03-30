namespace LicoresMaduro.API.Models.Auth;

/// <summary>
/// Represents a top-level application module stored in LM_Modules.
/// </summary>
public sealed class LmModule
{
    public int    ModuleId     { get; set; }
    public string ModuleName   { get; set; } = string.Empty;
    public string ModuleCode   { get; set; } = string.Empty;
    public string? Icon        { get; set; }
    public int    DisplayOrder { get; set; }
    public bool   IsActive     { get; set; } = true;

    // Navigation
    public ICollection<LmSubmodule> Submodules { get; set; } = [];
}
