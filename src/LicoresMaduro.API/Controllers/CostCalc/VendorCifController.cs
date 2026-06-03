using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/vendor-cif")]
[Authorize]
[Produces("application/json")]
public sealed class VendorCifController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPermissionService   _permissions;

    public VendorCifController(ApplicationDbContext db, IPermissionService permissions)
    { _db = db; _permissions = permissions; }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var data = await _db.CcVendorCifs
            .AsNoTracking()
            .OrderBy(x => x.VcifVendor)
            .Select(x => x.VcifVendor)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<string>>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VendorCifDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "COST_VENDOR_CIF", "WRITE", ct))
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.VendorCode))
            return BadRequest(ApiResponse.Fail("Vendor code is required."));

        var code = dto.VendorCode.Trim().ToUpper();

        if (await _db.CcVendorCifs.AnyAsync(x => x.VcifVendor == code, ct))
            return Conflict(ApiResponse.Fail($"Vendor code '{code}' already exists."));

        _db.CcVendorCifs.Add(new CcVendorCif { VcifVendor = code });
        await _db.SaveChangesAsync(ct);
        return StatusCode(201, ApiResponse<string>.Ok(code, "Created."));
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "COST_VENDOR_CIF", "DELETE", ct))
            return Forbid();

        var normalized = code.Trim().ToUpper();
        var item = await _db.CcVendorCifs
            .FirstOrDefaultAsync(x => x.VcifVendor == normalized, ct);
        if (item is null)
            return NotFound(ApiResponse.Fail($"Vendor '{normalized}' not found."));

        _db.CcVendorCifs.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record VendorCifDto(string VendorCode);
