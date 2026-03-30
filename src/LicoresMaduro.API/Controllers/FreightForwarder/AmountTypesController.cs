using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/amount-types")]
[Authorize]
[Produces("application/json")]
public sealed class AmountTypesController : ControllerBase
{
    private readonly ApplicationDbContext           _db;
    private readonly ILogger<AmountTypesController> _logger;

    public AmountTypesController(ApplicationDbContext db, ILogger<AmountTypesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.AmountTypes.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.AtCode.Contains(search) || x.AtDescription.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.AtCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<AmountType>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.AmountTypes.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Amount type {id} not found.")) : Ok(ApiResponse<AmountType>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] AmountTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new AmountType { AtCode = dto.AtCode, AtDescription = dto.AtDescription, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.AmountTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("AmountType '{Code}' created", dto.AtCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.AtId }, ApiResponse<AmountType>.Ok(entity, "Amount type created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] AmountTypeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.AmountTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Amount type {id} not found."));
        entity.AtCode = dto.AtCode; entity.AtDescription = dto.AtDescription;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<AmountType>.Ok(entity, "Amount type updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.AmountTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Amount type {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Amount type {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.AmountTypes.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Amount type {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("AmountType {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Amount type deleted."));
    }
}

public sealed record AmountTypeDto(string AtCode, string AtDescription);
