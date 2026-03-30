using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LicoresMaduro.API.Middleware;

/// <summary>
/// Supplemental JWT middleware: reads the Bearer token from the Authorization header,
/// validates it and attaches the ClaimsPrincipal to HttpContext.User so downstream
/// code can access custom claims before the MVC pipeline executes.
/// The built-in JwtBearer handler in Program.cs is the primary validator;
/// this middleware adds extra logging and IP capture for the audit trail.
/// </summary>
public sealed class JwtMiddleware
{
    private readonly RequestDelegate             _next;
    private readonly IConfiguration              _config;
    private readonly ILogger<JwtMiddleware>      _logger;

    public JwtMiddleware(
        RequestDelegate        next,
        IConfiguration         config,
        ILogger<JwtMiddleware> logger)
    {
        _next   = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context);

        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                AttachUserFromToken(context, token);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "JWT middleware: token validation skipped (built-in handler takes over)");
            }
        }

        await _next(context);
    }

    private static string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is not null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        // Also check query string for Swagger / WebSocket scenarios
        return context.Request.Query.TryGetValue("access_token", out var qsToken)
            ? qsToken.FirstOrDefault()
            : null;
    }

    private void AttachUserFromToken(HttpContext context, string token)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey   = jwtSettings["SecretKey"]!;

        var handler    = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer           = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidateAudience         = true,
            ValidAudience            = jwtSettings["Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(token, parameters, out _);

        // Store IP address for audit logging
        var ip = context.Connection.RemoteIpAddress?.ToString()
              ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
              ?? "unknown";

        context.Items["JwtPrincipal"] = principal;
        context.Items["ClientIp"]     = ip;

        _logger.LogDebug("JWT middleware: token validated for {Name}", principal.Identity?.Name);
    }
}
