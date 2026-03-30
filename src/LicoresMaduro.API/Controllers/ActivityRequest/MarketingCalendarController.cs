using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.ActivityRequest;

[ApiController]
[Route("api/activity/marketing-calendar")]
[Authorize]
[Produces("application/json")]
public sealed class MarketingCalendarController : ControllerBase
{
    private readonly ApplicationDbContext                  _db;
    private readonly ILogger<MarketingCalendarController>  _logger;

    public MarketingCalendarController(ApplicationDbContext db, ILogger<MarketingCalendarController> logger)
    { _db = db; _logger = logger; }

    // GET /api/activity/marketing-calendar?year=2026&search=Heineken
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int?    year            = null,
        [FromQuery] string? search          = null,
        [FromQuery] bool    includeInactive = false,
        [FromQuery] int     page            = 1,
        [FromQuery] int     pageSize        = 100,
        CancellationToken   ct              = default)
    {
        var q = _db.MarketingCalendars.AsNoTracking();
        if (!includeInactive) q = q.Where(x => x.IsActive);
        if (year.HasValue)    q = q.Where(x => x.McYear == year.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.McBrand.Contains(search)
                           || (x.McSupplierName != null && x.McSupplierName.Contains(search))
                           || (x.McSupplierCode != null && x.McSupplierCode.Contains(search)));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.McSupplierName)
                           .ThenBy(x => x.McBrand)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<MarketingCalendar>.Ok(data, page, pageSize, total));
    }

    // GET /api/activity/marketing-calendar/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.MarketingCalendars.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Marketing calendar record {id} not found."))
            : Ok(ApiResponse<MarketingCalendar>.Ok(e));
    }

    // GET /api/activity/marketing-calendar/years — distinct years in the table
    [HttpGet("years")]
    public async Task<IActionResult> GetYears(CancellationToken ct)
    {
        var years = await _db.MarketingCalendars
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.McYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
        return Ok(ApiResponse<List<int>>.Ok(years));
    }

    // POST /api/activity/marketing-calendar
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] MarketingCalendarDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        // Duplicate check (same supplier + brand + year, active)
        var exists = await _db.MarketingCalendars.AnyAsync(
            x => x.IsActive && x.McYear == dto.McYear
              && x.McBrand == dto.McBrand
              && x.McSupplierCode == dto.McSupplierCode, ct);
        if (exists)
            return Conflict(ApiResponse.Fail("A record for this Supplier / Brand / Year already exists."));

        var entity = new MarketingCalendar
        {
            McYear         = dto.McYear,
            McSupplierCode = dto.McSupplierCode,
            McSupplierName = dto.McSupplierName,
            McBrand        = dto.McBrand,
            McBudget       = dto.McBudget,
            McMonth1       = dto.McMonth1,
            McMonth2       = dto.McMonth2,
            McMonth3       = dto.McMonth3,
            McMonth4       = dto.McMonth4,
            McMonth5       = dto.McMonth5,
            McMonth6       = dto.McMonth6,
            McMonth7       = dto.McMonth7,
            McMonth8       = dto.McMonth8,
            McMonth9       = dto.McMonth9,
            McMonth10      = dto.McMonth10,
            McMonth11      = dto.McMonth11,
            McMonth12      = dto.McMonth12,
            McNotes        = dto.McNotes,
            IsActive       = true,
            CreatedAt      = DateTime.UtcNow
        };
        _db.MarketingCalendars.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("MarketingCalendar {Supplier}/{Brand}/{Year} created (id={Id})",
            entity.McSupplierCode, entity.McBrand, entity.McYear, entity.McId);
        return CreatedAtAction(nameof(GetById), new { id = entity.McId },
            ApiResponse<MarketingCalendar>.Ok(entity, "Marketing calendar record created."));
    }

    // PUT /api/activity/marketing-calendar/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] MarketingCalendarDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var entity = await _db.MarketingCalendars.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Marketing calendar record {id} not found."));

        // Duplicate check (exclude self)
        var exists = await _db.MarketingCalendars.AnyAsync(
            x => x.IsActive && x.McId != id
              && x.McYear == dto.McYear
              && x.McBrand == dto.McBrand
              && x.McSupplierCode == dto.McSupplierCode, ct);
        if (exists)
            return Conflict(ApiResponse.Fail("Another record for this Supplier / Brand / Year already exists."));

        entity.McYear         = dto.McYear;
        entity.McSupplierCode = dto.McSupplierCode;
        entity.McSupplierName = dto.McSupplierName;
        entity.McBrand        = dto.McBrand;
        entity.McBudget       = dto.McBudget;
        entity.McMonth1       = dto.McMonth1;
        entity.McMonth2       = dto.McMonth2;
        entity.McMonth3       = dto.McMonth3;
        entity.McMonth4       = dto.McMonth4;
        entity.McMonth5       = dto.McMonth5;
        entity.McMonth6       = dto.McMonth6;
        entity.McMonth7       = dto.McMonth7;
        entity.McMonth8       = dto.McMonth8;
        entity.McMonth9       = dto.McMonth9;
        entity.McMonth10      = dto.McMonth10;
        entity.McMonth11      = dto.McMonth11;
        entity.McMonth12      = dto.McMonth12;
        entity.McNotes        = dto.McNotes;

        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<MarketingCalendar>.Ok(entity, "Marketing calendar record updated."));
    }

    // PATCH /api/activity/marketing-calendar/{id}/toggle
    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var entity = await _db.MarketingCalendars.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Marketing calendar record {id} not found."));
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok($"Record {id} is now {(entity.IsActive ? "active" : "inactive")}."));
    }

    // DELETE /api/activity/marketing-calendar/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.MarketingCalendars.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Marketing calendar record {id} not found."));
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("MarketingCalendar {Id} soft-deleted", id);
        return Ok(ApiResponse.Ok("Marketing calendar record deleted."));
    }
}

public sealed record MarketingCalendarDto(
    int      McYear,
    string?  McSupplierCode,
    string?  McSupplierName,
    string   McBrand,
    decimal? McBudget,
    string?  McMonth1,
    string?  McMonth2,
    string?  McMonth3,
    string?  McMonth4,
    string?  McMonth5,
    string?  McMonth6,
    string?  McMonth7,
    string?  McMonth8,
    string?  McMonth9,
    string?  McMonth10,
    string?  McMonth11,
    string?  McMonth12,
    string?  McNotes
);
