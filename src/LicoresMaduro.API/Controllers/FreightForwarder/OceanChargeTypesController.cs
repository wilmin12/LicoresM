using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/ocean-charge-types")]
[Authorize]
[Produces("application/json")]
public sealed class OceanChargeTypesController : ControllerBase
{
    private readonly ApplicationDbContext                _db;
    private readonly ILogger<OceanChargeTypesController> _logger;

    public OceanChargeTypesController(ApplicationDbContext db, ILogger<OceanChargeTypesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.OceanFreightChargeTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.OfctCode.Contains(search) || x.OfctDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.OfctCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<OceanFreightChargeType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.OceanFreightChargeTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Ocean charge type {id} not found.")) : Ok(ApiResponse<OceanFreightChargeType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] OceanChargeTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new OceanFreightChargeType { OfctCode = dto.OfctCode, OfctDescription = dto.OfctDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.OceanFreightChargeTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("OceanChargeType '{Code}' created", dto.OfctCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.OfctId }, ApiResponse<OceanFreightChargeType>.Ok(entity, "Ocean charge type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] OceanChargeTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.OceanFreightChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Ocean charge type {id} not found."));
        entity.OfctCode = dto.OfctCode; entity.OfctDescription = dto.OfctDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<OceanFreightChargeType>.Ok(entity, "Ocean charge type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.OceanFreightChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Ocean charge type {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Ocean charge type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.OceanFreightChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Ocean charge type {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("OceanChargeType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Ocean charge type deleted."));
    }
}

public sealed record OceanChargeTypeDto(string OfctCode, string OfctDescription);
