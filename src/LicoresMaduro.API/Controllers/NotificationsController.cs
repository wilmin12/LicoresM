using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db) => _db = db;

    private int? CurrentUserId()
    {
        var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
             ?? User.FindFirst("sub");
        return c is not null && int.TryParse(c.Value, out var id) ? id : null;
    }

    // ── GET /api/notifications ─────────────────────────────────────────────────
    // Returns the latest 50 notifications for the logged-in user.
    // ?unread=true → only unread ones
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool unread = false, CancellationToken ct = default)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var q = _db.LmNotifications.AsNoTracking()
                   .Where(n => n.NtfUserId == userId);

        if (unread) q = q.Where(n => !n.NtfIsRead);

        var data = await q
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new
            {
                n.NtfId, n.NtfTitle, n.NtfMessage, n.NtfType,
                n.NtfIsRead, n.NtfUrl, n.NtfRefId, n.NtfRefType, n.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(data));
    }

    // ── GET /api/notifications/count ───────────────────────────────────────────
    [HttpGet("count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var count = await _db.LmNotifications
            .CountAsync(n => n.NtfUserId == userId && !n.NtfIsRead, ct);

        return Ok(ApiResponse<object>.Ok(new { Count = count }));
    }

    // ── PATCH /api/notifications/{id}/read ────────────────────────────────────
    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var n = await _db.LmNotifications
            .FirstOrDefaultAsync(x => x.NtfId == id && x.NtfUserId == userId, ct);

        if (n is null) return NotFound(ApiResponse.Fail("Notification not found."));

        n.NtfIsRead = true;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Marked as read."));
    }

    // ── PATCH /api/notifications/read-all ─────────────────────────────────────
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        await _db.LmNotifications
            .Where(n => n.NtfUserId == userId && !n.NtfIsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.NtfIsRead, true), ct);

        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }
}
