using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.SystemConfig;

[ApiController]
[Route("api/chat")]
[Authorize]
[Produces("application/json")]
public sealed class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ChatController(ApplicationDbContext db) { _db = db; }

    private int? CurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    // GET api/chat/history/{otherUserId}?skip=0&take=50
    [HttpGet("history/{otherUserId:int}")]
    public async Task<IActionResult> History(int otherUserId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var myId = CurrentUserId();
        if (myId is null) return Unauthorized();

        var messages = await _db.LmChatMessages
            .Where(m => (m.FromUserId == myId && m.ToUserId == otherUserId) ||
                        (m.FromUserId == otherUserId && m.ToUserId == myId))
            .OrderByDescending(m => m.SentAt)
            .Skip(skip).Take(take)
            .Select(m => new
            {
                m.MessageId,
                m.FromUserId,
                m.ToUserId,
                m.Message,
                m.SentAt,
                m.IsRead
            })
            .ToListAsync(ct);

        // Mark incoming as read
        await _db.LmChatMessages
            .Where(m => m.FromUserId == otherUserId && m.ToUserId == myId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), ct);

        return Ok(ApiResponse<object>.Ok(messages.OrderBy(m => m.SentAt)));
    }

    // GET api/chat/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var myId = CurrentUserId();
        if (myId is null) return Unauthorized();

        var count = await _db.LmChatMessages
            .CountAsync(m => m.ToUserId == myId && !m.IsRead, ct);

        return Ok(ApiResponse<object>.Ok(new { Count = count }));
    }

    // GET api/chat/contacts  — all users (except self) with IsOnline flag
    [HttpGet("contacts")]
    public async Task<IActionResult> Contacts(CancellationToken ct)
    {
        var myId   = CurrentUserId();
        if (myId is null) return Unauthorized();

        var cutoff  = DateTime.UtcNow.AddMinutes(-5);
        var onlineIds = await _db.LmSessions
            .Where(s => s.IsActive && s.LastSeenAt >= cutoff)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);

        var users = await _db.LmUsers
            .Where(u => u.UserId != myId && u.IsActive)
            .OrderBy(u => u.FullName ?? u.Username)
            .Select(u => new
            {
                u.UserId,
                FullName = u.FullName ?? u.Username,
                u.Username,
                IsOnline = onlineIds.Contains(u.UserId)
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(users));
    }

    // GET api/chat/conversations  — one entry per contact
    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(CancellationToken ct)
    {
        var myId = CurrentUserId();
        if (myId is null) return Unauthorized();

        // Get all users I've chatted with + last message
        var sent = await _db.LmChatMessages
            .Where(m => m.FromUserId == myId || m.ToUserId == myId)
            .Include(m => m.FromUser)
            .Include(m => m.ToUser)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync(ct);

        var contacts = sent
            .GroupBy(m => m.FromUserId == myId ? m.ToUserId : m.FromUserId)
            .Select(g =>
            {
                var last    = g.First();
                var contact = last.FromUserId == myId ? last.ToUser : last.FromUser;
                var unread  = g.Count(m => m.ToUserId == myId && !m.IsRead);
                return new
                {
                    UserId   = contact?.UserId,
                    FullName = contact?.FullName ?? contact?.Username ?? "?",
                    Username = contact?.Username,
                    LastMessage = last.Message,
                    LastAt      = last.SentAt,
                    Unread      = unread
                };
            })
            .OrderByDescending(c => c.LastAt)
            .ToList();

        return Ok(ApiResponse<object>.Ok(contacts));
    }
}
