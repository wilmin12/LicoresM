using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

/// <summary>
/// Full CRUD for the VENDORS catalog (Aankoopbon module).
/// Vendors are shared across Freight Forwarder and Aankoopbon.
/// </summary>
[ApiController]
[Route("api/aankoopbon/vendors")]
[Authorize]
[Produces("application/json")]
public sealed class VendorsController : ControllerBase
{
    private readonly ApplicationDbContext      _db;
    private readonly IHttpContextAccessor      _httpCtx;
    private readonly ILogger<VendorsController> _logger;

    public VendorsController(
        ApplicationDbContext db,
        IHttpContextAccessor httpCtx,
        ILogger<VendorsController> logger)
    {
        _db      = db;
        _httpCtx = httpCtx;
        _logger  = logger;
    }

    // ── GET /api/aankoopbon/vendors ────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search          = null,
        [FromQuery] bool    includeInactive = false,
        [FromQuery] int     page            = 1,
        [FromQuery] int     pageSize        = 50,
        CancellationToken   ct              = default)
    {
        var query = _db.Vendors.AsNoTracking();

        if (!includeInactive) query = query.Where(v => v.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v =>
                v.VndName.Contains(search) ||
                v.VndCode.Contains(search) ||
                (v.VndEmail != null && v.VndEmail.Contains(search)));

        var total   = await query.CountAsync(ct);
        var vendors = await query
            .OrderBy(v => v.VndName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(PagedResponse<Vendor>.Ok(vendors, page, pageSize, total));
    }

    // ── GET /api/aankoopbon/vendors/{id} ──────────────────────────────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var vendor = await _db.Vendors.FindAsync([id], ct);
        if (vendor is null)
            return NotFound(ApiResponse.Fail($"Vendor {id} not found."));

        return Ok(ApiResponse<Vendor>.Ok(vendor));
    }

    // ── GET /api/aankoopbon/vendors/by-code/{code} ────────────────────────────

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var vendor = await _db.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VndCode == code, ct);

        if (vendor is null)
            return NotFound(ApiResponse.Fail($"Vendor code '{code}' not found."));

        return Ok(ApiResponse<Vendor>.Ok(vendor));
    }

    // ── POST /api/aankoopbon/vendors ──────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,PurchaseManager")]
    public async Task<IActionResult> Create([FromBody] VendorCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        if (await _db.Vendors.AnyAsync(v => v.VndCode == dto.VndCode, ct))
            return Conflict(ApiResponse.Fail($"Vendor code '{dto.VndCode}' already exists."));

        var vendor = new Vendor
        {
            VndCode     = dto.VndCode,
            VndName     = dto.VndName,
            VndAddress1 = dto.VndAddress1,
            VndPhone1   = dto.VndPhone1,
            VndEmail    = dto.VndEmail,
            VndContact  = dto.VndContact,
            VndCurr     = dto.VndCurr,
            VndCrib     = dto.VndCrib,
            VndKvk      = dto.VndKvk,
            VndCash            = dto.VndCash,
            VndQuoteMandatory  = dto.VndQuoteMandatory,
            IsActive           = true,
            CreatedAt   = DateTime.UtcNow
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Vendor '{Code}' created by '{Caller}'",
            dto.VndCode, User.Identity?.Name);

        return CreatedAtAction(nameof(GetById),
            new { id = vendor.VndId },
            ApiResponse<Vendor>.Ok(vendor, "Vendor created."));
    }

    // ── PUT /api/aankoopbon/vendors/{id} ──────────────────────────────────────

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,PurchaseManager")]
    public async Task<IActionResult> Update(int id, [FromBody] VendorUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(
                ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var vendor = await _db.Vendors.FindAsync([id], ct);
        if (vendor is null)
            return NotFound(ApiResponse.Fail($"Vendor {id} not found."));

        vendor.VndName     = dto.VndName;
        vendor.VndAddress1 = dto.VndAddress1;
        vendor.VndPhone1   = dto.VndPhone1;
        vendor.VndEmail    = dto.VndEmail;
        vendor.VndContact  = dto.VndContact;
        vendor.VndCurr     = dto.VndCurr;
        vendor.VndCrib     = dto.VndCrib;
        vendor.VndKvk      = dto.VndKvk;
        vendor.VndCash           = dto.VndCash;
        vendor.VndQuoteMandatory = dto.VndQuoteMandatory;
        vendor.IsActive          = dto.IsActive;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Vendor>.Ok(vendor, "Vendor updated."));
    }

    // ── PATCH /api/aankoopbon/vendors/{id}/toggle ─────────────────────────────

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var vendor = await _db.Vendors.FindAsync([id], ct);
        if (vendor is null) return NotFound(ApiResponse.Fail($"Vendor {id} not found."));
        vendor.IsActive = !vendor.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Vendor {id} is now {(vendor.IsActive ? "active" : "inactive")}."));
    }

    // ── DELETE /api/aankoopbon/vendors/{id} ───────────────────────────────────

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var vendor = await _db.Vendors.FindAsync([id], ct);
        if (vendor is null)
            return NotFound(ApiResponse.Fail($"Vendor {id} not found."));

        vendor.IsActive = false;    // soft delete
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Vendor {Id} soft-deleted by '{Caller}'", id, User.Identity?.Name);
        return Ok(ApiResponse.Ok("Vendor deleted."));
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record VendorCreateDto(
    string  VndCode,
    string  VndName,
    string? VndAddress1,
    string? VndPhone1,
    string? VndEmail,
    string? VndContact,
    string? VndCurr,
    string? VndCrib,
    string? VndKvk,
    bool    VndCash,
    bool    VndQuoteMandatory
);

public sealed record VendorUpdateDto(
    string  VndName,
    string? VndAddress1,
    string? VndPhone1,
    string? VndEmail,
    string? VndContact,
    string? VndCurr,
    string? VndCrib,
    string? VndKvk,
    bool    VndCash,
    bool    VndQuoteMandatory,
    bool    IsActive
);
