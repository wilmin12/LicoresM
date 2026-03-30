namespace LicoresMaduro.API.Models.Auth;

public sealed class LmSession
{
    public int       SessionId  { get; set; }
    public string    SessionKey { get; set; } = string.Empty;
    public int       UserId     { get; set; }
    public DateTime  LoginAt    { get; set; }
    public DateTime  LastSeenAt { get; set; }
    public DateTime? LogoutAt   { get; set; }
    public string?   IpAddress  { get; set; }
    public string?   UserAgent  { get; set; }
    public bool      IsActive   { get; set; } = true;

    // Navigation
    public LmUser? User { get; set; }
}
