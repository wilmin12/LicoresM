using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/charge-pers")]
[Authorize]
[Produces("application/json")]
public sealed class ChargePersController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly ILogger<ChargePersController> _logger;

    public ChargePersController(ApplicationDbContext db, ILogger<ChargePersController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.ChargePers.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.CpCode.Contains(search) || x.CpDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.CpCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<ChargePer>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ChargePers.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Charge per {id} not found.")) : Ok(ApiResponse<ChargePer>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ChargePerDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new ChargePer { CpCode = dto.CpCode, CpDescription = dto.CpDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ChargePers.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ChargePer '{Code}' created", dto.CpCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.CpId }, ApiResponse<ChargePer>.Ok(entity, "Charge per created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ChargePerDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.ChargePers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge per {id} not found."));
        entity.CpCode = dto.CpCode; entity.CpDescription = dto.CpDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ChargePer>.Ok(entity, "Charge per updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.ChargePers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge per {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Charge per {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ChargePers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Charge per {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ChargePer {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Charge per deleted."));
    }
}

public sealed record ChargePerDto(string CpCode, string CpDescription);
