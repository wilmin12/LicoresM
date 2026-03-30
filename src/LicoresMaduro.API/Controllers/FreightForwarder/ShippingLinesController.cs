using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/shipping-lines")]
[Authorize]
[Produces("application/json")]
public sealed class ShippingLinesController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly ILogger<ShippingLinesController> _logger;

    public ShippingLinesController(ApplicationDbContext db, ILogger<ShippingLinesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.ShippingLines.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.SlCode.Contains(search) || x.SlName.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.SlName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<ShippingLine>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ShippingLines.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Shipping line {id} not found.")) : Ok(ApiResponse<ShippingLine>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ShippingLineDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new ShippingLine { SlCode = dto.SlCode, SlName = dto.SlName, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ShippingLines.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ShippingLine '{Code}' created", dto.SlCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.SlId }, ApiResponse<ShippingLine>.Ok(entity, "Shipping line created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ShippingLineDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.ShippingLines.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Shipping line {id} not found."));
        entity.SlCode = dto.SlCode; entity.SlName = dto.SlName;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ShippingLine>.Ok(entity, "Shipping line updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.ShippingLines.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Shipping line {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Shipping line {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ShippingLines.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Shipping line {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ShippingLine {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Shipping line deleted."));
    }
}

public sealed record ShippingLineDto(string SlCode, string SlName);
