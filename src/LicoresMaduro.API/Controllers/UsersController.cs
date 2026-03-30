using LicoresMaduro.API.Data;
using LicoresMaduro.API.DTOs.Auth;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicoresMaduro.API.Controllers;

/// <summary>
/// User management CRUD (requires authentication; SuperAdmin/Admin roles for write ops).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IAuthService            _authService;
    private readonly ApplicationDbContext    _db;
    private readonly IWebHostEnvironment     _env;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IAuthService authService, ApplicationDbContext db, IWebHostEnvironment env, ILogger<UsersController> logger)
    {
        _authService = authService;
        _db          = db;
        _env         = env;
        _logger      = logger;
    }

    // ── GET /api/users ─────────────────────────────────────────────────────────

    /// <summary>List all users.</summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _authService.GetAllUsersAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<UserResponseDto>>.Ok(users));
    }

    // ── GET /api/users/{id} ────────────────────────────────────────────────────

    /// <summary>Get a single user by ID.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var user = await _authService.GetUserByIdAsync(id, ct);
        if (user is null)
            return NotFound(ApiResponse.Fail($"User {id} not found."));

        return Ok(ApiResponse<UserResponseDto>.Ok(user));
    }

    // ── POST /api/users ────────────────────────────────────────────────────────

    /// <summary>Create a new user.</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        try
        {
            var created = await _authService.CreateUserAsync(dto, ct);
            _logger.LogInformation("User '{Username}' created by '{Caller}'",
                dto.Username, User.Identity?.Name);
            return CreatedAtAction(nameof(GetById),
                new { id = created.UserId },
                ApiResponse<UserResponseDto>.Ok(created, "User created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail(ex.Message));
        }
    }

    // ── PUT /api/users/{id} ────────────────────────────────────────────────────

    /// <summary>Update an existing user.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        try
        {
            var updated = await _authService.UpdateUserAsync(id, dto, ct);
            return Ok(ApiResponse<UserResponseDto>.Ok(updated, "User updated successfully."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail($"User {id} not found."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail(ex.Message));
        }
    }

    // ── PUT /api/users/{id}/password ────────────────────────────────────────────

    /// <summary>Change a user's password.</summary>
    [HttpPut("{id:int}/password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        // Users can only change their own password unless they are Admin/SuperAdmin
        var callerIdClaim = User.FindFirst("sub")?.Value
                         ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var callerId = int.TryParse(callerIdClaim, out var pid) ? pid : -1;

        var isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Admin");
        if (!isAdmin && callerId != id)
            return Forbid();

        try
        {
            await _authService.ChangePasswordAsync(id, dto, ct);
            return Ok(ApiResponse.Ok("Password changed successfully."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail($"User {id} not found."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail(ex.Message));
        }
    }

    // ── PUT /api/users/{id}/toggle-status ──────────────────────────────────────

    /// <summary>Enable or disable a user account.</summary>
    [HttpPut("{id:int}/toggle-status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        try
        {
            await _authService.ToggleUserStatusAsync(id, ct);
            return Ok(ApiResponse.Ok("User status toggled."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail($"User {id} not found."));
        }
    }

    // ── POST /api/users/{id}/avatar ────────────────────────────────────────────

    /// <summary>Upload a profile photo for a user (own or admin).</summary>
    [HttpPost("{id:int}/avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(int id, IFormFile file, CancellationToken ct)
    {
        // Users can only upload their own avatar; admins can upload for anyone
        var callerIdClaim = User.FindFirst("sub")?.Value
                         ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var callerId = int.TryParse(callerIdClaim, out var pid) ? pid : -1;
        var isAdmin  = User.IsInRole("SuperAdmin") || User.IsInRole("Admin");
        if (!isAdmin && callerId != id) return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(ApiResponse.Fail("File must not exceed 2 MB."));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext))
            return BadRequest(ApiResponse.Fail("Only image files are allowed (.jpg, .png, .gif, .webp)."));

        var user = await _db.LmUsers.FindAsync(new object[] { id }, ct);
        if (user is null) return NotFound(ApiResponse.Fail($"User {id} not found."));

        // Save to frontend/img/avatars/
        var uploadsDir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "..", "frontend", "img", "avatars"));
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"user_{id}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream, ct);

        user.AvatarUrl = $"/img/avatars/{fileName}";
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Avatar updated for user {UserId}", id);
        return Ok(ApiResponse<object>.Ok(new { AvatarUrl = user.AvatarUrl }, "Avatar updated successfully."));
    }

    // ── DELETE /api/users/{id} ─────────────────────────────────────────────────

    /// <summary>Soft-delete a user (sets IsActive = false).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _authService.DeleteUserAsync(id, ct);
            _logger.LogWarning("User {UserId} soft-deleted by '{Caller}'", id, User.Identity?.Name);
            return Ok(ApiResponse.Ok("User deleted successfully."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail($"User {id} not found."));
        }
    }
}
