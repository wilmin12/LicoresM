using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/requestors-vendor")]
[Authorize]
[Produces("application/json")]
public sealed class RequestorsVendorController : ControllerBase
{
    private readonly ApplicationDbContext                _db;
    private readonly IPermissionService                  _permissions;
    private readonly ILogger<RequestorsVendorController> _logger;

    public RequestorsVendorController(ApplicationDbContext db, IPermissionService permissions, ILogger<RequestorsVendorController> logger)
    { _db = db; _permissions = permissions; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 500, CancellationToken ct = default)
    {
        var q = _db.RequestorVendors.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.RsRequestor.Contains(search) || x.RsVendor.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RsRequestor).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<RequestorVendor>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.RequestorVendors.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Requestor-vendor {id} not found.")) : Ok(ApiResponse<RequestorVendor>.Ok(e));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RequestorVendorDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_REQUESTORS_VENDOR", "WRITE", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new RequestorVendor { RsRequestor = dto.RsRequestor, RsVendor = dto.RsVendor, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.RequestorVendors.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("RequestorVendor link created: '{Requestor}' -> '{Vendor}'", dto.RsRequestor, dto.RsVendor);
        return CreatedAtAction(nameof(GetById), new { id = entity.RvId }, ApiResponse<RequestorVendor>.Ok(entity, "Requestor-vendor link created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RequestorVendorDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_REQUESTORS_VENDOR", "EDIT", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.RequestorVendors.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor-vendor {id} not found."));
        entity.RsRequestor = dto.RsRequestor; entity.RsVendor = dto.RsVendor;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<RequestorVendor>.Ok(entity, "Requestor-vendor link updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_REQUESTORS_VENDOR", "EDIT", ct))
            return Forbid();
        var entity = await _db.RequestorVendors.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor-vendor {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Requestor-vendor {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_REQUESTORS_VENDOR", "DELETE", ct))
            return Forbid();
        var entity = await _db.RequestorVendors.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Requestor-vendor {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("RequestorVendor {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Requestor-vendor link deleted."));
    }
}

public sealed record RequestorVendorDto(string RsRequestor, string RsVendor);
