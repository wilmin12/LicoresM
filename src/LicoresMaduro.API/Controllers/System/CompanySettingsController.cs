using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.SysConfig;

[ApiController]
[Route("api/system/company-settings")]
[Authorize]
[Produces("application/json")]
public sealed class CompanySettingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment  _env;

    public CompanySettingsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db  = db;
        _env = env;
    }

    /// <summary>Returns the company profile. Public — used by login page and sidebar before auth.</summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var settings = await _db.CompanySettings.FirstOrDefaultAsync(ct)
                       ?? new CompanySettings { CsId = 1, CsCompanyName = "Licores Maduro" };
        return Ok(ApiResponse<CompanySettings>.Ok(settings));
    }

    /// <summary>Creates or updates the company profile (upsert).</summary>
    [HttpPut, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update([FromBody] CompanySettingsDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            return BadRequest(ApiResponse.Fail("Company name is required."));

        var settings = await _db.CompanySettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new CompanySettings { CsId = 1 };
            _db.CompanySettings.Add(settings);
        }

        settings.CsCompanyName = dto.CompanyName.Trim();
        settings.CsLegalName   = dto.LegalName?.Trim();
        settings.CsTagline     = dto.Tagline?.Trim();
        settings.CsRnc         = dto.Rnc?.Trim();
        settings.CsAddress     = dto.Address?.Trim();
        settings.CsCity        = dto.City?.Trim();
        settings.CsCountry     = dto.Country?.Trim();
        settings.CsPhone       = dto.Phone?.Trim();
        settings.CsPhone2      = dto.Phone2?.Trim();
        settings.CsEmail       = dto.Email?.Trim();
        settings.CsWebsite     = dto.Website?.Trim();
        settings.CsUpdatedAt   = DateTime.UtcNow;
        settings.CsUpdatedBy   = User.Identity?.Name;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CompanySettings>.Ok(settings, "Saved successfully."));
    }

    /// <summary>Uploads a new company logo and stores its URL.</summary>
    [HttpPost("logo"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("No file uploaded."));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".svg" or ".webp"))
            return BadRequest(ApiResponse.Fail("Allowed formats: PNG, JPG, SVG, WEBP."));

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(ApiResponse.Fail("File size must not exceed 2 MB."));

        // Save into the frontend/uploads/logo/ folder (served as static files)
        var frontendPath = Path.GetFullPath(
            Path.Combine(_env.ContentRootPath, "..", "..", "frontend"));
        var uploadDir = Path.Combine(frontendPath, "uploads", "logo");
        Directory.CreateDirectory(uploadDir);

        var fileName = "company-logo" + ext;
        var filePath = Path.Combine(uploadDir, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream, ct);

        var logoUrl = $"/uploads/logo/{fileName}";

        var settings = await _db.CompanySettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new CompanySettings { CsId = 1, CsCompanyName = "Licores Maduro" };
            _db.CompanySettings.Add(settings);
        }
        settings.CsLogoUrl   = logoUrl;
        settings.CsUpdatedAt = DateTime.UtcNow;
        settings.CsUpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { logoUrl }, "Logo uploaded."));
    }
}

public record CompanySettingsDto(
    string  CompanyName,
    string? LegalName,
    string? Tagline,
    string? Rnc,
    string? Address,
    string? City,
    string? Country,
    string? Phone,
    string? Phone2,
    string? Email,
    string? Website);
