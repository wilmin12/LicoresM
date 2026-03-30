using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace LicoresMaduro.API.Controllers.SystemConfig;

[ApiController]
[Route("api/system/email-config")]
[Authorize]
[Produces("application/json")]
public sealed class EmailConfigController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EmailConfigController> _log;

    public EmailConfigController(ApplicationDbContext db, ILogger<EmailConfigController> log)
    {
        _db  = db;
        _log = log;
    }

    // GET api/system/email-config
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var cfg = await _db.LmEmailConfig.AsNoTracking().FirstOrDefaultAsync(ct);
        if (cfg is null) return NotFound(ApiResponse.Fail("Email configuration not found."));

        // Mask password before sending to client
        var result = new
        {
            cfg.ConfigId,
            cfg.SmtpHost,
            cfg.SmtpPort,
            cfg.UseSsl,
            cfg.SenderName,
            cfg.SenderEmail,
            HasPassword    = !string.IsNullOrEmpty(cfg.SenderPassword),
            cfg.Recipients,
            cfg.StaleOrderDays,
            cfg.IsEnabled,
            cfg.UpdatedAt,
            cfg.UpdatedBy
        };

        return Ok(ApiResponse<object>.Ok(result));
    }

    // PUT api/system/email-config
    [HttpPut]
    public async Task<IActionResult> Save([FromBody] EmailConfigDto dto, CancellationToken ct)
    {
        var cfg = await _db.LmEmailConfig.FirstOrDefaultAsync(ct);

        if (cfg is null)
        {
            cfg = new Models.Auth.LmEmailConfig { ConfigId = 1 };
            _db.LmEmailConfig.Add(cfg);
        }

        cfg.SmtpHost       = dto.SmtpHost?.Trim()    ?? string.Empty;
        cfg.SmtpPort       = dto.SmtpPort;
        cfg.UseSsl         = dto.UseSsl;
        cfg.SenderName     = dto.SenderName?.Trim()  ?? string.Empty;
        cfg.SenderEmail    = dto.SenderEmail?.Trim() ?? string.Empty;
        cfg.Recipients     = dto.Recipients?.Trim()  ?? string.Empty;
        cfg.StaleOrderDays = dto.StaleOrderDays > 0 ? dto.StaleOrderDays : 4;
        cfg.IsEnabled      = dto.IsEnabled;
        cfg.UpdatedAt      = DateTime.UtcNow;
        cfg.UpdatedBy      = User.Identity?.Name;

        // Only update password if a new one was provided
        if (!string.IsNullOrWhiteSpace(dto.SenderPassword))
            cfg.SenderPassword = dto.SenderPassword;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Email configuration saved."));
    }

    // POST api/system/email-config/test
    [HttpPost("test")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailDto dto, CancellationToken ct)
    {
        var cfg = await _db.LmEmailConfig.AsNoTracking().FirstOrDefaultAsync(ct);
        if (cfg is null || string.IsNullOrEmpty(cfg.SmtpHost))
            return BadRequest(ApiResponse.Fail("SMTP configuration is not set up."));

        if (string.IsNullOrEmpty(cfg.SenderPassword))
            return BadRequest(ApiResponse.Fail("SMTP password is not configured."));

        var toAddress = dto.To?.Trim();
        if (string.IsNullOrEmpty(toAddress))
            return BadRequest(ApiResponse.Fail("Recipient email is required."));

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(cfg.SenderName, cfg.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = "Licores Maduro — Test Email";
            message.Body    = new TextPart("html")
            {
                Text = $"<p>This is a test email from the <strong>Licores Maduro</strong> system.</p><p>Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>"
            };

            using var client = new SmtpClient();
            // Auto: port 465 → SslOnConnect, port 587 → StartTls, others → best effort
            var sslOption = cfg.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, sslOption, ct);
            await client.AuthenticateAsync(cfg.SenderEmail, cfg.SenderPassword, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _log.LogInformation("Test email sent to {To} by {User}", toAddress, User.Identity?.Name);
            return Ok(ApiResponse.Ok($"Test email sent to {toAddress}."));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Test email failed");
            return BadRequest(ApiResponse.Fail($"Failed to send email: {ex.Message}"));
        }
    }
}

public sealed class EmailConfigDto
{
    public string SmtpHost       { get; set; } = string.Empty;
    public int    SmtpPort       { get; set; } = 587;
    public bool   UseSsl         { get; set; } = true;
    public string SenderName     { get; set; } = string.Empty;
    public string SenderEmail    { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string Recipients     { get; set; } = string.Empty;
    public int    StaleOrderDays { get; set; } = 4;
    public bool   IsEnabled      { get; set; } = false;
}

public sealed class TestEmailDto
{
    public string To { get; set; } = string.Empty;
}
