using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/inland-charge-types")]
[Authorize]
[Produces("application/json")]
public sealed class InlandChargeTypesController : ControllerBase
{
    private readonly ApplicationDbContext                 _db;
    private readonly ILogger<InlandChargeTypesController> _logger;

    public InlandChargeTypesController(ApplicationDbContext db, ILogger<InlandChargeTypesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.InlandFreightChargeTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.IfctCode.Contains(search) || x.IfctDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.IfctCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<InlandFreightChargeType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.InlandFreightChargeTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Inland charge type {id} not found.")) : Ok(ApiResponse<InlandFreightChargeType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] InlandChargeTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new InlandFreightChargeType { IfctCode = dto.IfctCode, IfctDescription = dto.IfctDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.InlandFreightChargeTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("InlandChargeType '{Code}' created", dto.IfctCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.IfctId }, ApiResponse<InlandFreightChargeType>.Ok(entity, "Inland charge type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] InlandChargeTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.InlandFreightChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Inland charge type {id} not found."));
        entity.IfctCode = dto.IfctCode; entity.IfctDescription = dto.IfctDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<InlandFreightChargeType>.Ok(entity, "Inland charge type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.InlandFreightChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Inland charge type {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Inland charge type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.InlandFreightChargeTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Inland charge type {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("InlandChargeType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Inland charge type deleted."));
    }
}

public sealed record InlandChargeTypeDto(string IfctCode, string IfctDescription);
