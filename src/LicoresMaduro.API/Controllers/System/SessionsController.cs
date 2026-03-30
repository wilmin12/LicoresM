using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.SystemConfig;

[ApiController]
[Route("api/sessions")]
[Authorize]
[Produces("application/json")]
public sealed class SessionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SessionsController(ApplicationDbContext db) { _db = db; }

    // ── Helper: resolve UserId from JWT ───────────────────────────────────────
    private int? CurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    // POST api/sessions/open  — called by frontend right after login
    [HttpPost("open")]
    public async Task<IActionResult> Open(CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var sessionKey = Guid.NewGuid().ToString("N");
        var ip         = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua         = Request.Headers.UserAgent.ToString();
        if (ua.Length > 300) ua = ua[..300];

        var session = new LmSession
        {
            SessionKey = sessionKey,
            UserId     = userId.Value,
            LoginAt    = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IpAddress  = ip,
            UserAgent  = ua,
            IsActive   = true
        };

        _db.LmSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { SessionKey = sessionKey }));
    }

    // POST api/sessions/heartbeat  — called every 2 minutes by frontend
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] SessionKeyDto dto, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(dto.SessionKey)) return BadRequest();

        var session = await _db.LmSessions
            .FirstOrDefaultAsync(s => s.SessionKey == dto.SessionKey && s.IsActive, ct);

        if (session is null) return NotFound();

        session.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok());
    }

    // POST api/sessions/close  — called on logout
    [HttpPost("close")]
    public async Task<IActionResult> Close([FromBody] SessionKeyDto dto, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(dto.SessionKey)) return Ok(ApiResponse.Ok());

        var session = await _db.LmSessions
            .FirstOrDefaultAsync(s => s.SessionKey == dto.SessionKey && s.IsActive, ct);

        if (session is not null)
        {
            session.IsActive  = false;
            session.LogoutAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse.Ok("Session closed."));
    }

    // GET api/sessions/active  — SuperAdmin only
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);

        var sessions = await _db.LmSessions
            .Include(s => s.User!).ThenInclude(u => u.Role)
            .Where(s => s.IsActive && s.LastSeenAt >= cutoff)
            .OrderByDescending(s => s.LastSeenAt)
            .Select(s => new
            {
                s.SessionId,
                s.SessionKey,
                s.UserId,
                Username    = s.User!.Username,
                FullName    = s.User.FullName,
                RoleName    = s.User.Role!.RoleName,
                s.LoginAt,
                s.LastSeenAt,
                s.IpAddress,
                MinutesAgo  = (int)(DateTime.UtcNow - s.LastSeenAt).TotalMinutes
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { Sessions = sessions, Total = sessions.Count }));
    }

    // DELETE api/sessions/{id}  — force-close a session (SuperAdmin only)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ForceClose(int id, CancellationToken ct)
    {
        var session = await _db.LmSessions.FindAsync([id], ct);
        if (session is null) return NotFound(ApiResponse.Fail("Session not found."));

        session.IsActive = false;
        session.LogoutAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok("Session terminated."));
    }
}

public sealed class SessionKeyDto
{
    public string SessionKey { get; set; } = string.Empty;
}
