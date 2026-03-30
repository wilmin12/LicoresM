using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/container-types")]
[Authorize]
[Produces("application/json")]
public sealed class ContainerTypesController : ControllerBase
{
    private readonly ApplicationDbContext           _db;
    private readonly ILogger<ContainerTypesController> _logger;

    public ContainerTypesController(ApplicationDbContext db, ILogger<ContainerTypesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.ContainerTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.CtCode.Contains(search) || x.CtDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.CtCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<ContainerType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ContainerTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Container type {id} not found.")) : Ok(ApiResponse<ContainerType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ContainerTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new ContainerType
        {
            CtCode           = dto.CtCode,
            CtDescription    = dto.CtDescription,
            CtContainerSpecs = dto.CtContainerSpecs,
            CtCases          = dto.CtCases,
            CtWghtKilogram   = dto.CtWghtKilogram,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow
        };
        _db.ContainerTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ContainerType '{Code}' created", dto.CtCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.CtId }, ApiResponse<ContainerType>.Ok(entity, "Container type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ContainerTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.ContainerTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container type {id} not found."));
        entity.CtCode = dto.CtCode; entity.CtDescription = dto.CtDescription;
        entity.CtContainerSpecs = dto.CtContainerSpecs; entity.CtCases = dto.CtCases; entity.CtWghtKilogram = dto.CtWghtKilogram;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ContainerType>.Ok(entity, "Container type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.ContainerTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container type {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Container type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ContainerTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container type {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ContainerType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Container type deleted."));
    }
}

public sealed record ContainerTypeDto(
    string  CtCode,
    string  CtDescription,
    string? CtContainerSpecs,
    int?    CtCases,
    int?    CtWghtKilogram
);
