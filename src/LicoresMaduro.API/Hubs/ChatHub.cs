using LicoresMaduro.API.Data;
using LicoresMaduro.API.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ChatHub>     _log;

    public ChatHub(ApplicationDbContext db, ILogger<ChatHub> log)
    {
        _db  = db;
        _log = log;
    }

    // ── Resolve UserId from JWT claims ────────────────────────────────────────
    private int? CurrentUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                 ?? Context.User?.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    // ── On connect: join personal group ───────────────────────────────────────
    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId();
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
            _log.LogDebug("ChatHub: user {UserId} connected ({ConnId})", userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId();
        if (userId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        await base.OnDisconnectedAsync(exception);
    }

    // ── Send message to another user ──────────────────────────────────────────
    public async Task SendMessage(int toUserId, string text)
    {
        var fromUserId = CurrentUserId();
        if (fromUserId is null || string.IsNullOrWhiteSpace(text)) return;

        text = text.Trim();
        if (text.Length > 1000) text = text[..1000];

        // Persist
        var msg = new LmChatMessage
        {
            FromUserId = fromUserId.Value,
            ToUserId   = toUserId,
            Message    = text,
            SentAt     = DateTime.UtcNow,
            IsRead     = false
        };
        _db.LmChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        // Load sender info
        var sender = await _db.LmUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == fromUserId.Value);

        var payload = new
        {
            msg.MessageId,
            msg.FromUserId,
            msg.ToUserId,
            msg.Message,
            SentAt      = msg.SentAt,
            FromName    = sender?.FullName ?? sender?.Username ?? "?",
            FromInitials = Initials(sender?.FullName ?? sender?.Username ?? "?")
        };

        // Deliver to recipient
        await Clients.Group(UserGroup(toUserId)).SendAsync("ReceiveMessage", payload);
        // Echo back to sender (other tabs / this tab)
        await Clients.Group(UserGroup(fromUserId.Value)).SendAsync("ReceiveMessage", payload);
    }

    // ── Mark messages from a user as read ────────────────────────────────────
    public async Task MarkRead(int fromUserId)
    {
        var myId = CurrentUserId();
        if (myId is null) return;

        await _db.LmChatMessages
            .Where(m => m.FromUserId == fromUserId && m.ToUserId == myId.Value && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

        // Notify the current user their unread count changed
        await Clients.Group(UserGroup(myId.Value)).SendAsync("UnreadCountChanged");
    }

    // ── Typing indicator ──────────────────────────────────────────────────────
    public async Task Typing(int toUserId)
    {
        var fromUserId = CurrentUserId();
        if (fromUserId is null) return;
        await Clients.Group(UserGroup(toUserId)).SendAsync("UserTyping", fromUserId.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string UserGroup(int userId) => $"user-{userId}";

    private static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Take(2).Select(w => w[0]))
              .ToUpperInvariant();
}
