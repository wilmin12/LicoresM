using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LicoresMaduro.API.Data;
using LicoresMaduro.API.DTOs.Auth;
using LicoresMaduro.API.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LicoresMaduro.API.Services;

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<UserResponseDto>   CreateUserAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<UserResponseDto>   UpdateUserAsync(int userId, UpdateUserDto dto, CancellationToken ct = default);
    Task                    ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task                    ToggleUserStatusAsync(int userId, CancellationToken ct = default);
    Task<UserResponseDto?>  GetUserByIdAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserResponseDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task                    DeleteUserAsync(int userId, CancellationToken ct = default);
}

// ── Implementation ─────────────────────────────────────────────────────────────

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration      _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _db     = db;
        _config = config;
        _logger = logger;
    }

    // ── Login ──────────────────────────────────────────────────────────────────

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var user = await _db.LmUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, ct);

        if (user is null)
        {
            _logger.LogWarning("Login failed: user '{Username}' not found or inactive", request.Username);
            return null;
        }

        const int TargetWorkFactor = 10;

        // Special case: placeholder hash from seed (first-run)
        bool passwordValid;
        if (user.PasswordHash.StartsWith("$PLACEHOLDER", StringComparison.Ordinal))
        {
            // Allow login with "Admin@123" and immediately re-hash
            passwordValid = request.Password == "Admin@123";
            if (passwordValid)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, TargetWorkFactor);
                _db.LmUsers.Update(user);
            }
        }
        else
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            // Rehash silently if stored hash uses a higher work factor
            if (passwordValid && BCrypt.Net.BCrypt.PasswordNeedsRehash(user.PasswordHash, TargetWorkFactor))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, TargetWorkFactor);
                _db.LmUsers.Update(user);
            }
        }

        if (!passwordValid)
        {
            _logger.LogWarning("Login failed: invalid password for '{Username}'", request.Username);
            return null;
        }

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Build permissions (role base + user overrides)
        var permissions = await GetPermissionsAsync(user.RoleId, user.UserId, ct);

        // Generate JWT
        var (token, expiresAt) = GenerateToken(user);

        _logger.LogInformation("User '{Username}' logged in successfully", user.Username);

        return new LoginResponseDto(
            Token: token,
            ExpiresAt: expiresAt,
            User: new UserInfoDto(
                UserId:      user.UserId,
                Username:    user.Username,
                FullName:    user.FullName,
                Email:       user.Email,
                RoleName:    user.Role?.RoleName ?? string.Empty,
                RoleId:      user.RoleId,
                AvatarUrl:   user.AvatarUrl,
                Permissions: permissions
            )
        );
    }

    // ── CRUD ───────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<UserResponseDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        return await _db.LmUsers
            .Include(u => u.Role)
            .OrderBy(u => u.Username)
            .Select(u => MapToResponse(u))
            .ToListAsync(ct);
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.LmUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        return user is null ? null : MapToResponse(user);
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        if (await _db.LmUsers.AnyAsync(u => u.Username == dto.Username, ct))
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");

        if (await _db.LmUsers.AnyAsync(u => u.Email == dto.Email, ct))
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

        if (!await _db.LmRoles.AnyAsync(r => r.RoleId == dto.RoleId && r.IsActive, ct))
            throw new InvalidOperationException($"Role ID {dto.RoleId} does not exist or is inactive.");

        var user = new LmUser
        {
            Username     = dto.Username,
            Email        = dto.Email,
            FullName     = dto.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 10),
            RoleId       = dto.RoleId,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        _db.LmUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(user).Reference(u => u.Role).LoadAsync(ct);
        return MapToResponse(user);
    }

    public async Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _db.LmUsers.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (await _db.LmUsers.AnyAsync(u => u.Username == dto.Username && u.UserId != userId, ct))
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");

        if (await _db.LmUsers.AnyAsync(u => u.Email == dto.Email && u.UserId != userId, ct))
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

        user.Username = dto.Username;
        user.Email    = dto.Email;
        user.FullName = dto.FullName;
        user.RoleId   = dto.RoleId;
        user.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);
        await _db.Entry(user).Reference(u => u.Role).LoadAsync(ct);
        return MapToResponse(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.LmUsers.FindAsync([userId], ct)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, 10);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ToggleUserStatusAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.LmUsers.FindAsync([userId], ct)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.LmUsers.FindAsync([userId], ct)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (user.IsActive)
            throw new InvalidOperationException("Cannot delete an active user. Deactivate the user first.");

        // Clean up all related records before removing the user
        var sessions = await _db.LmSessions
            .Where(s => s.UserId == userId).ToListAsync(ct);
        _db.LmSessions.RemoveRange(sessions);

        var notifications = await _db.LmNotifications
            .Where(n => n.NtfUserId == userId).ToListAsync(ct);
        _db.LmNotifications.RemoveRange(notifications);

        var chatMessages = await _db.LmChatMessages
            .Where(m => m.FromUserId == userId || m.ToUserId == userId).ToListAsync(ct);
        _db.LmChatMessages.RemoveRange(chatMessages);

        _db.LmUsers.Remove(user);
        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(int roleId, int userId, CancellationToken ct)
    {
        // Load role-level permissions (base)
        var rolePerms = await _db.LmRolePermissions
            .Include(p => p.Submodule)
                .ThenInclude(s => s!.Module)
            .Where(p => p.RoleId == roleId && p.Submodule!.IsActive)
            .ToListAsync(ct);

        // Load user-level overrides
        var userOverrides = await _db.LmUserPermissions
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.SubmoduleId, ct);

        return rolePerms.Select(p =>
        {
            // If a user-level override exists for this submodule, use it
            if (userOverrides.TryGetValue(p.SubmoduleId, out var ov))
                return new PermissionDto(
                    p.SubmoduleId,
                    p.Submodule!.SubmoduleName,
                    p.Submodule.SubmoduleCode,
                    p.Submodule.Module!.ModuleCode,
                    ov.CanAccess,
                    ov.CanRead,
                    ov.CanWrite,
                    ov.CanEdit,
                    ov.CanDelete,
                    ov.CanApprove
                );

            return new PermissionDto(
                p.SubmoduleId,
                p.Submodule!.SubmoduleName,
                p.Submodule.SubmoduleCode,
                p.Submodule.Module!.ModuleCode,
                p.CanAccess,
                p.CanRead,
                p.CanWrite,
                p.CanEdit,
                p.CanDelete,
                p.CanApprove
            );
        }).ToList();
    }

    private (string token, DateTime expiresAt) GenerateToken(LmUser user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey   = jwtSettings["SecretKey"]!;
        var issuer      = jwtSettings["Issuer"]!;
        var audience    = jwtSettings["Audience"]!;
        var hours       = int.Parse(jwtSettings["ExpirationHours"] ?? "8");
        var expiresAt   = DateTime.UtcNow.AddHours(hours);

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim("username",                    user.Username),
            new Claim("email",                       user.Email),
            new Claim("fullName",                    user.FullName),
            new Claim("roleId",                      user.RoleId.ToString()),
            new Claim(ClaimTypes.Role,               user.Role?.RoleName ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static UserResponseDto MapToResponse(LmUser u) => new(
        UserId:    u.UserId,
        Username:  u.Username,
        Email:     u.Email,
        FullName:  u.FullName,
        IsActive:  u.IsActive,
        CreatedAt: u.CreatedAt,
        LastLogin: u.LastLogin,
        RoleId:    u.RoleId,
        RoleName:  u.Role?.RoleName ?? string.Empty,
        AvatarUrl: u.AvatarUrl
    );
}
