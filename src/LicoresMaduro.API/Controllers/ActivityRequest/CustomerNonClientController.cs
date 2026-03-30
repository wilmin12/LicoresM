using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/customer-non-client")]
[Authorize]
[Produces("application/json")]
public sealed class CustomerNonClientController : ControllerBase
{
    private readonly ApplicationDbContext                _db;
    private readonly ILogger<CustomerNonClientController> _logger;
    public CustomerNonClientController(ApplicationDbContext db, ILogger<CustomerNonClientController> logger) { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool includeInactive = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.CustomerNonClients.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<CustomerNonClient>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.CustomerNonClients.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Customer non-client {id} not found.")) : Ok(ApiResponse<CustomerNonClient>.Ok(e));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CodeNameDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new CustomerNonClient { Code = dto.Code, Name = dto.Name, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.CustomerNonClients.Add(entity); await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.CncId }, ApiResponse<CustomerNonClient>.Ok(entity, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CodeNameDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.CustomerNonClients.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Customer non-client {id} not found."));
        entity.Code = dto.Code; entity.Name = dto.Name; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CustomerNonClient>.Ok(entity, "Updated."));
    }

    [HttpPatch("{id:int}/toggle"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.CustomerNonClients.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Customer non-client {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Customer non-client {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.CustomerNonClients.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Customer non-client {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("CustomerNonClient {Id} soft-deleted", id); return Ok(ApiResponse.Ok("Deleted."));
    }
}

/// <summary>Shared DTO for catalogs with Code + Name fields.</summary>
public sealed record CodeNameDto(string Code, string Name);

/// <summary>Shared DTO for catalogs with Code + Description fields.</summary>
public sealed record CodeDescriptionDto(string Code, string Description);
