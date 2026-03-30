using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/lcl-charge-types")]
[Authorize]
[Produces("application/json")]
public sealed class LclChargeTypesController : ControllerBase
{
    private readonly ApplicationDbContext             _db;
    private readonly ILogger<LclChargeTypesController> _logger;

    public LclChargeTypesController(ApplicationDbContext db, ILogger<LclChargeTypesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.LclChargeTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.LctCode.Contains(search) || x.LctDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.LctCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<LclChargeType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.LclChargeTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"LCL charge type {id} not found.")) : Ok(ApiResponse<LclChargeType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] LclChargeTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new LclChargeType { LctCode = dto.LctCode, LctDescription = dto.LctDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.LclChargeTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("LclChargeType '{Code}' created", dto.LctCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.LctId }, ApiResponse<LclChargeType>.Ok(entity, "LCL charge type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] LclChargeTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.LclChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL charge type {id} not found."));
        entity.LctCode = dto.LctCode; entity.LctDescription = dto.LctDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LclChargeType>.Ok(entity, "LCL charge type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.LclChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL charge type {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"LCL charge type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.LclChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"LCL charge type {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("LclChargeType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("LCL charge type deleted."));
    }
}

public sealed record LclChargeTypeDto(string LctCode, string LctDescription);
