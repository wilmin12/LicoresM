using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/budget-activities")]
[Authorize]
[Produces("application/json")]
public sealed class BudgetActivitiesController : ControllerBase
{
    private readonly ApplicationDbContext               _db;
    private readonly ILogger<BudgetActivitiesController> _logger;

    public BudgetActivitiesController(ApplicationDbContext db, ILogger<BudgetActivitiesController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.BudgetActivities.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Code.Contains(search) || x.Description.Contains(search) || (x.BillingDescr != null && x.BillingDescr.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.DisplaySeq).ThenBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<BudgetActivity>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.BudgetActivities.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Budget activity {id} not found.")) : Ok(ApiResponse<BudgetActivity>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] BudgetActivityDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new BudgetActivity
        {
            Code         = dto.Code,
            Description  = dto.Description,
            BillingDescr = dto.BillingDescr,
            DisplaySeq   = dto.DisplaySeq,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        _db.BudgetActivities.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("BudgetActivity '{Code}' created", dto.Code);
        return CreatedAtAction(nameof(GetById), new { id = entity.BaId }, ApiResponse<BudgetActivity>.Ok(entity, "Budget activity created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] BudgetActivityDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.BudgetActivities.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Budget activity {id} not found."));
        entity.Code = dto.Code; entity.Description = dto.Description;
        entity.BillingDescr = dto.BillingDescr; entity.DisplaySeq = dto.DisplaySeq;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<BudgetActivity>.Ok(entity, "Budget activity updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.BudgetActivities.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Budget activity {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Budget activity {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.BudgetActivities.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Budget activity {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("BudgetActivity {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Budget activity deleted."));
    }
}

public sealed record BudgetActivityDto(string Code, string Description, string? BillingDescr, short? DisplaySeq);
