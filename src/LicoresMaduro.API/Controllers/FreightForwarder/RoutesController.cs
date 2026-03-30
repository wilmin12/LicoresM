using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/routes")]
[Authorize]
[Produces("application/json")]
public sealed class RoutesController : ControllerBase
{
    private readonly ApplicationDbContext      _db;
    private readonly ILogger<RoutesController> _logger;

    public RoutesController(ApplicationDbContext db, ILogger<RoutesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.Routes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.RouCode.Contains(search) || x.RouDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RouCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<LicoresMaduro.API.Data.Route>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Routes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Route {id} not found.")) : Ok(ApiResponse<LicoresMaduro.API.Data.Route>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] RouteDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new LicoresMaduro.API.Data.Route { RouCode = dto.RouCode, RouDescription = dto.RouDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Routes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Route '{Code}' created", dto.RouCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.RouId }, ApiResponse<LicoresMaduro.API.Data.Route>.Ok(entity, "Route created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] RouteDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.Routes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Route {id} not found."));
        entity.RouCode = dto.RouCode; entity.RouDescription = dto.RouDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LicoresMaduro.API.Data.Route>.Ok(entity, "Route updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.Routes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Route {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Route {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Routes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Route {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Route {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Route deleted."));
    }
}

public sealed record RouteDto(string RouCode, string RouDescription);
