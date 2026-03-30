using LicoresMaduro.API.DTOs.Auth;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicoresMaduro.API.Controllers;

/// <summary>
/// Authentication endpoints: login, logout, and current-user info.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService            _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger      = logger;
    }

    /// <summary>Authenticate a user and return a JWT token.</summary>
    /// <response code="200">Login successful – returns token and user info.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var result = await _authService.LoginAsync(request, ct);
        if (result is null)
            return Unauthorized(ApiResponse.Fail("Invalid username or password."));

        _logger.LogInformation("Successful login for user '{Username}'", request.Username);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful."));
    }

    /// <summary>
    /// Logout – client should discard the token.
    /// Server-side there is nothing to invalidate with stateless JWT.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // For a stateless JWT strategy the token is discarded client-side.
        // Extend this endpoint with a token blocklist / Redis blacklist if needed.
        _logger.LogInformation("User '{Name}' logged out", User.Identity?.Name);
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }

    /// <summary>Return the currently authenticated user's profile and permissions.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token claims."));

        var user = await _authService.GetUserByIdAsync(userId, ct);
        if (user is null)
            return NotFound(ApiResponse.Fail($"User {userId} not found."));

        return Ok(ApiResponse<UserResponseDto>.Ok(user));
    }
}
