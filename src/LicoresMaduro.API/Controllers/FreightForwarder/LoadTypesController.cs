using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/load-types")]
[Authorize]
[Produces("application/json")]
public sealed class LoadTypesController : ControllerBase
{
    private readonly ApplicationDbContext        _db;
    private readonly ILogger<LoadTypesController> _logger;

    public LoadTypesController(ApplicationDbContext db, ILogger<LoadTypesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.LoadTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.LtCode.Contains(search) || x.LtDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.LtCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<LoadType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.LoadTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Load type {id} not found.")) : Ok(ApiResponse<LoadType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] LoadTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new LoadType { LtCode = dto.LtCode, LtDescription = dto.LtDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.LoadTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("LoadType '{Code}' created", dto.LtCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.LtId }, ApiResponse<LoadType>.Ok(entity, "Load type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] LoadTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.LoadTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Load type {id} not found."));
        entity.LtCode = dto.LtCode; entity.LtDescription = dto.LtDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<LoadType>.Ok(entity, "Load type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.LoadTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Load type {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Load type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.LoadTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Load type {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("LoadType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Load type deleted."));
    }
}

public sealed record LoadTypeDto(string LtCode, string LtDescription);
