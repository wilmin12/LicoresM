using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/shipping-agents")]
[Authorize]
[Produces("application/json")]
public sealed class ShippingAgentsController : ControllerBase
{
    private readonly ApplicationDbContext            _db;
    private readonly ILogger<ShippingAgentsController> _logger;

    public ShippingAgentsController(ApplicationDbContext db, ILogger<ShippingAgentsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.ShippingAgents.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.SaCode.Contains(search) || x.SaName.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.SaName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<ShippingAgent>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.ShippingAgents.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Shipping agent {id} not found.")) : Ok(ApiResponse<ShippingAgent>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ShippingAgentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new ShippingAgent { SaCode = dto.SaCode, SaName = dto.SaName, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ShippingAgents.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("ShippingAgent '{Code}' created", dto.SaCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.SaId }, ApiResponse<ShippingAgent>.Ok(entity, "Shipping agent created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ShippingAgentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.ShippingAgents.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Shipping agent {id} not found."));
        entity.SaCode = dto.SaCode; entity.SaName = dto.SaName;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ShippingAgent>.Ok(entity, "Shipping agent updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.ShippingAgents.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Shipping agent {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Shipping agent {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ShippingAgents.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Shipping agent {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("ShippingAgent {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Shipping agent deleted."));
    }
}

public sealed record ShippingAgentDto(string SaCode, string SaName);
