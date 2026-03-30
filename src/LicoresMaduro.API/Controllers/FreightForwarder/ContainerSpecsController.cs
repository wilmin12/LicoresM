using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/container-specs")]
[Authorize]
[Produces("application/json")]
public sealed class ContainerSpecsController : ControllerBase
{
    private readonly ApplicationDbContext           _db;
    private readonly ILogger<ContainerSpecsController> _logger;

    public ContainerSpecsController(ApplicationDbContext db, ILogger<ContainerSpecsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.ContainerSpecs.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.CsCode.Contains(search) || x.CsDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.CsCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<ContainerSpec>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ContainerSpecs.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Container spec {id} not found.")) : Ok(ApiResponse<ContainerSpec>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ContainerSpecDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new ContainerSpec { CsCode = dto.CsCode, CsDescription = dto.CsDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ContainerSpecs.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ContainerSpec '{Code}' created", dto.CsCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.CsId }, ApiResponse<ContainerSpec>.Ok(entity, "Container spec created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ContainerSpecDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.ContainerSpecs.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container spec {id} not found."));
        entity.CsCode = dto.CsCode; entity.CsDescription = dto.CsDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ContainerSpec>.Ok(entity, "Container spec updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.ContainerSpecs.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container spec {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Container spec {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ContainerSpecs.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container spec {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ContainerSpec {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Container spec deleted."));
    }
}

public sealed record ContainerSpecDto(string CsCode, string CsDescription);
