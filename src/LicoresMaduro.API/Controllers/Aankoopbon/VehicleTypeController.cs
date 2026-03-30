using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/vehicle-types")]
[Authorize]
[Produces("application/json")]
public sealed class VehicleTypeController : ControllerBase
{
    private readonly ApplicationDbContext            _db;
    private readonly ILogger<VehicleTypeController> _logger;

    public VehicleTypeController(ApplicationDbContext db, ILogger<VehicleTypeController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.VehicleTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.VtName.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.VtName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<VehicleType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.VehicleTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Vehicle type {id} not found.")) : Ok(ApiResponse<VehicleType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] VehicleTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new VehicleType { VtName = dto.VtName, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.VehicleTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("VehicleType '{Name}' created", dto.VtName);
        return CreatedAtAction(nameof(GetById), new { id = entity.VtId }, ApiResponse<VehicleType>.Ok(entity, "Vehicle type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.VehicleTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vehicle type {id} not found."));
        entity.VtName = dto.VtName; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<VehicleType>.Ok(entity, "Vehicle type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.VehicleTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vehicle type {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Vehicle type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.VehicleTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vehicle type {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("VehicleType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Vehicle type deleted."));
    }
}

public sealed record VehicleTypeDto(string VtName);
