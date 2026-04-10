using System.Text;
using LicoresMaduro.API.Data;
using LicoresMaduro.API.Hubs;
using LicoresMaduro.API.Middleware;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// ── Bootstrap Serilog early ───────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Windows Service support ───────────────────────────────────────────────
    builder.Host.UseWindowsService();

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services));

    // ── Database ──────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null)));

    // ── DHW Database (read-only source for Cost Calculation) ──────────────────
    builder.Services.AddDbContext<DhwDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DHWConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null)));

    // ── JWT Authentication ────────────────────────────────────────────────────
    var jwtSection = builder.Configuration.GetSection("JwtSettings");
    var secretKey  = jwtSection["SecretKey"]
                     ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
    var issuer     = jwtSection["Issuer"]   ?? "LicoresMaduroAPI";
    var audience   = jwtSection["Audience"] ?? "LicoresMaduroFrontend";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // set true in production
        options.SaveToken            = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer           = true,
            ValidIssuer              = issuer,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
        // SignalR sends JWT as query string (WebSocket can't set headers)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/chatHub"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // ── CORS ──────────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LicoresPolicy", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
        });
    });

    // ── AutoMapper ────────────────────────────────────────────────────────────
    builder.Services.AddAutoMapper(typeof(Program).Assembly);

    // ── SignalR ───────────────────────────────────────────────────────────────
    builder.Services.AddSignalR();

    // ── Application Services ──────────────────────────────────────────────────
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPermissionService, PermissionService>();
    builder.Services.AddHttpContextAccessor();

    // ── Controllers ───────────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy        = null;  // keep PascalCase
            opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;  // tolerate any casing in request bodies
            opts.JsonSerializerOptions.ReferenceHandler            = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        })
        .ConfigureApiBehaviorOptions(opts =>
        {
            // Suppress [ApiController]'s automatic 400 ValidationProblemDetails so that
            // each controller's own "if (!ModelState.IsValid)" block runs and returns
            // our custom ApiResponse format with readable field-specific errors.
            opts.SuppressModelStateInvalidFilter = true;
        });

    // ── Swagger / OpenAPI ─────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "Licores Maduro API",
            Version     = "v1",
            Description = "Enterprise API for Licores Maduro management system",
            Contact     = new OpenApiContact
            {
                Name  = "Licores Maduro IT",
                Email = "it@licoresmaduro.com"
            }
        });

        // JWT security definition
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.ApiKey,
            Scheme       = "Bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter: Bearer {your JWT token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── Health Checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();
    // ─────────────────────────────────────────────────────────────────────────

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Licores Maduro API v1");
            c.RoutePrefix = "swagger";
        });
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    // Only redirect to HTTPS in production — mobile dev connects via HTTP
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseCors("LicoresPolicy");

    // Custom JWT middleware (optional – supplements the built-in bearer handler)
    app.UseMiddleware<JwtMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<ChatHub>("/chatHub");
    app.MapHealthChecks("/health");

    // ── Minimal error endpoint ────────────────────────────────────────────────
    app.Map("/error", () => Results.Problem());

    // ── Serve Frontend Static Files ───────────────────────────────────────────
    // In production (published): frontend is copied into the publish output as a subfolder.
    // In development: frontend lives two levels up from the API project folder.
    var frontendPath = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, "frontend"));
    if (!Directory.Exists(frontendPath))
        frontendPath = Path.GetFullPath(
            Path.Combine(builder.Environment.ContentRootPath, "..", "..", "frontend"));
    if (Directory.Exists(frontendPath))
    {
        var fp = new PhysicalFileProvider(frontendPath);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fp, RequestPath = "" });
        app.UseStaticFiles(new StaticFileOptions  { FileProvider = fp, RequestPath = "" });
        Log.Information("Serving frontend from: {Path}", frontendPath);
    }

    Log.Information("Licores Maduro API starting up...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
