namespace LicoresMaduro.API.Models.Auth;

public sealed class LmChatMessage
{
    public int      MessageId  { get; set; }
    public int      FromUserId { get; set; }
    public int      ToUserId   { get; set; }
    public string   Message    { get; set; } = string.Empty;
    public DateTime SentAt     { get; set; } = DateTime.UtcNow;
    public bool     IsRead     { get; set; } = false;

    // Navigation
    public LmUser? FromUser { get; set; }
    public LmUser? ToUser   { get; set; }
}
