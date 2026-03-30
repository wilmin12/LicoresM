using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/fiscal-years")]
[Authorize]
[Produces("application/json")]
public sealed class FiscalYearsController : ControllerBase
{
    private readonly ApplicationDbContext           _db;
    private readonly ILogger<FiscalYearsController> _logger;

    public FiscalYearsController(ApplicationDbContext db, ILogger<FiscalYearsController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null, [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.FiscalYears.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search) && int.TryParse(search, out var yr))
            q = q.Where(x => x.FyYear == yr);
        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(x => x.FyYear).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(PagedResponse<FiscalYear>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.FiscalYears.FindAsync([id], ct);
        return e is null ? NotFound(ApiResponse.Fail($"Fiscal year {id} not found.")) : Ok(ApiResponse<FiscalYear>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] FiscalYearDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = new FiscalYear
        {
            FyYear      = dto.FyYear,
            FyStartDate = dto.FyStartDate,
            FyEndDate   = dto.FyEndDate,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow
        };
        _db.FiscalYears.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("FiscalYear {Year} created", dto.FyYear);
        return CreatedAtAction(nameof(GetById), new { id = entity.FyId }, ApiResponse<FiscalYear>.Ok(entity, "Fiscal year created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] FiscalYearDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
        var entity = await _db.FiscalYears.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Fiscal year {id} not found."));
        entity.FyYear = dto.FyYear; entity.FyStartDate = dto.FyStartDate; entity.FyEndDate = dto.FyEndDate;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<FiscalYear>.Ok(entity, "Fiscal year updated."));
    }

    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.FiscalYears.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Fiscal year {id} not found."));
        entity.IsActive = !entity.IsActive; await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Fiscal year {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.FiscalYears.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Fiscal year {id} not found."));
        entity.IsActive = false; await _db.SaveChangesAsync(ct);
        _logger.LogWarning("FiscalYear {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Fiscal year deleted."));
    }
}

public sealed record FiscalYearDto(int FyYear, DateOnly FyStartDate, DateOnly FyEndDate);
