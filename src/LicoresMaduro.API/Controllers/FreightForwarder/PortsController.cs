using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/ports")]
[Authorize]
[Produces("application/json")]
public sealed class PortsController : ControllerBase
{
    private readonly ApplicationDbContext    _db;
    private readonly ILogger<PortsController> _logger;

    public PortsController(ApplicationDbContext db, ILogger<PortsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.PortsOfLoading.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.PlCode.Contains(search) || x.PlName.Contains(search) || (x.PlCountry != null && x.PlCountry.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.PlName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<PortOfLoading>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.PortsOfLoading.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Port {id} not found.")) : Ok(ApiResponse<PortOfLoading>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] PortOfLoadingDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new PortOfLoading { PlCode = dto.PlCode, PlName = dto.PlName, PlCountry = dto.PlCountry, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.PortsOfLoading.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Port '{Code}' created", dto.PlCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.PlId }, ApiResponse<PortOfLoading>.Ok(entity, "Port created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] PortOfLoadingDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.PortsOfLoading.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port {id} not found."));
        entity.PlCode = dto.PlCode; entity.PlName = dto.PlName; entity.PlCountry = dto.PlCountry;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<PortOfLoading>.Ok(entity, "Port updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.PortsOfLoading.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Port {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.PortsOfLoading.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Port {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Port {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Port deleted."));
    }
}

public sealed record PortOfLoadingDto(string PlCode, string PlName, string? PlCountry);
