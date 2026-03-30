namespace LicoresMaduro.API.Models.Auth;

public sealed class LmEmailConfig
{
    public int     ConfigId       { get; set; }
    public string  SmtpHost       { get; set; } = string.Empty;
    public int     SmtpPort       { get; set; } = 587;
    public bool    UseSsl         { get; set; } = true;
    public string  SenderName     { get; set; } = string.Empty;
    public string  SenderEmail    { get; set; } = string.Empty;
    public string  SenderPassword { get; set; } = string.Empty;
    /// <summary>Semicolon-separated list of recipient email addresses.</summary>
    public string  Recipients     { get; set; } = string.Empty;
    public int     StaleOrderDays { get; set; } = 4;
    public bool    IsEnabled      { get; set; } = false;
    public DateTime? UpdatedAt    { get; set; }
    public string?   UpdatedBy    { get; set; }
}
