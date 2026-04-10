using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Aankoopbon;

[ApiController]
[Route("api/aankoopbon/receivers")]
[Authorize]
[Produces("application/json")]
public sealed class ReceiversController : ControllerBase
{
    private readonly ApplicationDbContext          _db;
    private readonly IPermissionService            _permissions;
    private readonly ILogger<ReceiversController> _logger;

    public ReceiversController(ApplicationDbContext db, IPermissionService permissions, ILogger<ReceiversController> logger)
    { _db = db; _permissions = permissions; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.Receivers.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.RecName.Contains(search) || (x.RecIdDoc != null && x.RecIdDoc.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RecName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<Receiver>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.Receivers.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Receiver {id} not found.")) : Ok(ApiResponse<Receiver>.Ok(e));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReceiverDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_RECEIVERS", "WRITE", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new Receiver { RecName = dto.RecName, RecIdDoc = dto.RecIdDoc, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Receivers.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Receiver '{Name}' created", dto.RecName);
        return CreatedAtAction(nameof(GetById), new { id = entity.RecId }, ApiResponse<Receiver>.Ok(entity, "Receiver created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ReceiverDto dto, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_RECEIVERS", "EDIT", ct))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.Receivers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Receiver {id} not found."));
        entity.RecName = dto.RecName; entity.RecIdDoc = dto.RecIdDoc;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Receiver>.Ok(entity, "Receiver updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_RECEIVERS", "EDIT", ct))
            return Forbid();
        var entity = await _db.Receivers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Receiver {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Receiver {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(User, "AB_RECEIVERS", "DELETE", ct))
            return Forbid();
        var entity = await _db.Receivers.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Receiver {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Receiver {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Receiver deleted."));
    }
}

public sealed record ReceiverDto(string RecName, string? RecIdDoc);
