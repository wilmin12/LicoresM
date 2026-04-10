using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/vehicles")]
[Authorize]
[Produces("application/json")]
public sealed class VehiclesController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly IPermissionService            _permissions;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(ApplicationDbContext db, IPermissionService permissions, ILogger<VehiclesController> logger)
    { _db = db; _permissions = permissions; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.Vehicles.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.VhLicense.Contains(search) ||
                              (x.VhType != null && x.VhType.Contains(search)) ||
                              (x.VhModel != null && x.VhModel.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.VhLicense).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<Vehicle>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Vehicles.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Vehicle {id} not found.")) : Ok(ApiResponse<Vehicle>.Ok(e));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_VEHICLES", "WRITE", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new Vehicle { VhLicense = dto.VhLicense, VhType = dto.VhType, VhModel = dto.VhModel, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Vehicle '{License}' created", dto.VhLicense);
        return CreatedAtAction(nameof(GetById), new { id = entity.VhId }, ApiResponse<Vehicle>.Ok(entity, "Vehicle created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_VEHICLES", "EDIT", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.Vehicles.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vehicle {id} not found."));
        entity.VhLicense = dto.VhLicense; entity.VhType = dto.VhType; entity.VhModel = dto.VhModel;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Vehicle>.Ok(entity, "Vehicle updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_VEHICLES", "EDIT", ct))
            return Forbid();
        var entity = await _db.Vehicles.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vehicle {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Vehicle {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_VEHICLES", "DELETE", ct))
            return Forbid();
        var entity = await _db.Vehicles.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Vehicle {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Vehicle {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Vehicle deleted."));
    }
}

public sealed record VehicleDto(string VhLicense, string? VhType, string? VhModel);
