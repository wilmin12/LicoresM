namespace LicoresMaduro.API.Models.Auth;

/// <summary>
/// Audit trail record stored in LM_AuditLog.
/// All create/update/delete operations are logged here.
/// </summary>
public sealed class LmAuditLog
{
    public long    LogId     { get; set; }
    public int?    UserId    { get; set; }
    public string  Action    { get; set; } = string.Empty;   // CREATE | UPDATE | DELETE
    public string  TableName { get; set; } = string.Empty;
    public string? RecordId  { get; set; }
    public string? OldValues { get; set; }                   // JSON
    public string? NewValues { get; set; }                   // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
