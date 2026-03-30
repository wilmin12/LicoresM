using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Tracking;

[ApiController]
[Route("api/tracking/container-types")]
[Authorize]
[Produces("application/json")]
public sealed class ContainerTypesController : ControllerBase
{
    private readonly ApplicationDbContext             _db;
    private readonly ILogger<ContainerTypesController> _logger;

    public ContainerTypesController(ApplicationDbContext db, ILogger<ContainerTypesController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // GET api/tracking/container-types
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search          = null,
        [FromQuery] bool    includeInactive = false,
        [FromQuery] int     page            = 1,
        [FromQuery] int     pageSize        = 50,
        CancellationToken   ct              = default)
    {
        var query = _db.TrackingContainerTypes.AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.TctDescription.Contains(search) || x.TctCode.Contains(search));

        var total   = await query.CountAsync(ct);
        var records = await query.OrderBy(x => x.TctCode)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(PagedResponse<TrackingContainerType>.Ok(records, page, pageSize, total));
    }

    // GET api/tracking/container-types/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var entity = await _db.TrackingContainerTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container type {id} not found."));
        return Ok(ApiResponse<TrackingContainerType>.Ok(entity));
    }

    // POST api/tracking/container-types
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ContainerTypeDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(ApiResponse.Fail("Code is required."));
        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(ApiResponse.Fail("Description is required."));

        var entity = new TrackingContainerType
        {
            TctCode        = dto.Code.Trim().ToUpperInvariant(),
            TctDescription = dto.Description.Trim(),
            IsActive       = true,
            CreatedAt      = DateTime.UtcNow
        };
        _db.TrackingContainerTypes.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Conflict(ApiResponse.Fail($"A container type with code '{entity.TctCode}' already exists."));
        }

        _logger.LogInformation("ContainerType '{Code}' created", entity.TctCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.TctId }, ApiResponse<TrackingContainerType>.Ok(entity, "Container type created."));
    }

    // PUT api/tracking/container-types/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ContainerTypeDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(ApiResponse.Fail("Description is required."));

        var entity = await _db.TrackingContainerTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container type {id} not found."));

        if (!string.IsNullOrWhiteSpace(dto.Code))
            entity.TctCode = dto.Code.Trim().ToUpperInvariant();
        entity.TctDescription = dto.Description.Trim();
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Conflict(ApiResponse.Fail($"A container type with code '{entity.TctCode}' already exists."));
        }

        return Ok(ApiResponse<TrackingContainerType>.Ok(entity, "Container type updated."));
    }

    // PATCH api/tracking/container-types/{id}/toggle
    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.TrackingContainerTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container type {id} not found."));

        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Container type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    // DELETE api/tracking/container-types/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.TrackingContainerTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Container type {id} not found."));

        _db.TrackingContainerTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ContainerType {Id} permanently deleted", id);
        return Ok(ApiResponse.Ok("Container type deleted successfully."));
    }
}

public sealed class ContainerTypeDto
{
    public string? Code        { get; set; }
    public string? Description { get; set; }
}
