using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.FreightForwarder;

[ApiController]
[Route("api/freight/routes-by-agents")]
[Authorize]
[Produces("application/json")]
public sealed class RoutesByAgentsController : ControllerBase
{
    private readonly ApplicationDbContext             _db;
    private readonly ILogger<RoutesByAgentsController> _logger;

    public RoutesByAgentsController(ApplicationDbContext db, ILogger<RoutesByAgentsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.RoutesByShippingAgents.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.RsaPort.Contains(search) || x.RsaShippingAgent.Contains(search) || x.RsaRoute.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RsaPort).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<RouteByShippingAgent>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.RoutesByShippingAgents.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Route by agent {id} not found.")) : Ok(ApiResponse<RouteByShippingAgent>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] RouteByAgentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new RouteByShippingAgent
        {
            RsaPort          = dto.RsaPort,
            RsaShippingAgent = dto.RsaShippingAgent,
            RsaRoute         = dto.RsaRoute,
            RsaDays          = dto.RsaDays,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow
        };
        _db.RoutesByShippingAgents.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("RouteByAgent created for port '{Port}'", dto.RsaPort);
        return CreatedAtAction(nameof(GetById), new { id = entity.RsaId }, ApiResponse<RouteByShippingAgent>.Ok(entity, "Route by agent created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] RouteByAgentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.RoutesByShippingAgents.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Route by agent {id} not found."));
        entity.RsaPort = dto.RsaPort; entity.RsaShippingAgent = dto.RsaShippingAgent;
        entity.RsaRoute = dto.RsaRoute; entity.RsaDays = dto.RsaDays;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<RouteByShippingAgent>.Ok(entity, "Route by agent updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.RoutesByShippingAgents.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Route by agent {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Route by agent {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.RoutesByShippingAgents.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Route by agent {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("RouteByAgent {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Route by agent deleted."));
    }
}

public sealed record RouteByAgentDto(string RsaPort, string RsaShippingAgent, string RsaRoute, short? RsaDays);
