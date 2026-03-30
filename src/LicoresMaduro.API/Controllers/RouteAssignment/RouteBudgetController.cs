using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.RouteAssignment;

[ApiController]
[Route("api/route/budget")]
[Authorize]
[Produces("application/json")]
public sealed class RouteBudgetController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RouteBudgetController> _logger;

    public RouteBudgetController(ApplicationDbContext db, ILogger<RouteBudgetController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int?    year          = null,
        [FromQuery] string? accountNumber = null,
        [FromQuery] string? itemCode      = null,
        [FromQuery] int     page          = 1,
        [FromQuery] int     pageSize      = 50,
        CancellationToken ct = default)
    {
        var q = _db.RouteBudgets.AsNoTracking();
        if (year.HasValue)
            q = q.Where(x => x.RbYear == year.Value);
        if (!string.IsNullOrWhiteSpace(accountNumber))
            q = q.Where(x => x.RbAccountNumber.Contains(accountNumber));
        if (!string.IsNullOrWhiteSpace(itemCode))
            q = q.Where(x => x.RbItemCode.Contains(itemCode));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RbYear)
                           .ThenBy(x => x.RbAccountNumber)
                           .ThenBy(x => x.RbItemCode)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<RouteBudget>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.RouteBudgets.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Budget record {id} not found."))
            : Ok(ApiResponse<RouteBudget>.Ok(e));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] RouteBudgetDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        if (await _db.RouteBudgets.AnyAsync(x =>
                x.RbYear == dto.RbYear &&
                x.RbAccountNumber == dto.RbAccountNumber &&
                x.RbItemCode == dto.RbItemCode, ct))
            return Conflict(ApiResponse.Fail($"Budget record for Year={dto.RbYear}, Account={dto.RbAccountNumber}, Item={dto.RbItemCode} already exists."));

        var entity = MapToEntity(new RouteBudget(), dto);
        _db.RouteBudgets.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("RouteBudget created Y={Year} A={Acct} I={Item}", dto.RbYear, dto.RbAccountNumber, dto.RbItemCode);
        return CreatedAtAction(nameof(GetById), new { id = entity.RbId },
            ApiResponse<RouteBudget>.Ok(entity, "Budget created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] RouteBudgetDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var entity = await _db.RouteBudgets.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse.Fail($"Budget record {id} not found."));

        if ((entity.RbYear != dto.RbYear || entity.RbAccountNumber != dto.RbAccountNumber || entity.RbItemCode != dto.RbItemCode) &&
            await _db.RouteBudgets.AnyAsync(x =>
                x.RbYear == dto.RbYear &&
                x.RbAccountNumber == dto.RbAccountNumber &&
                x.RbItemCode == dto.RbItemCode &&
                x.RbId != id, ct))
            return Conflict(ApiResponse.Fail("Duplicate key (Year, Account, Item)."));

        MapToEntity(entity, dto);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<RouteBudget>.Ok(entity, "Budget updated."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await _db.RouteBudgets.FindAsync([id], ct);
        if (e is null) return NotFound(ApiResponse.Fail($"Budget record {id} not found."));
        _db.RouteBudgets.Remove(e);
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("RouteBudget {Id} deleted", id);
        return Ok(ApiResponse.Ok("Budget record deleted."));
    }

    private static RouteBudget MapToEntity(RouteBudget e, RouteBudgetDto d)
    {
        e.RbYear          = d.RbYear;
        e.RbAccountNumber = d.RbAccountNumber;
        e.RbItemCode      = d.RbItemCode;
        e.RbQty01 = d.RbQty01; e.RbQty02 = d.RbQty02; e.RbQty03 = d.RbQty03;
        e.RbQty04 = d.RbQty04; e.RbQty05 = d.RbQty05; e.RbQty06 = d.RbQty06;
        e.RbQty07 = d.RbQty07; e.RbQty08 = d.RbQty08; e.RbQty09 = d.RbQty09;
        e.RbQty10 = d.RbQty10; e.RbQty11 = d.RbQty11; e.RbQty12 = d.RbQty12;
        return e;
    }
}

public sealed record RouteBudgetDto(
    int     RbYear,
    string  RbAccountNumber,
    string  RbItemCode,
    decimal? RbQty01 = 0, decimal? RbQty02 = 0, decimal? RbQty03 = 0,
    decimal? RbQty04 = 0, decimal? RbQty05 = 0, decimal? RbQty06 = 0,
    decimal? RbQty07 = 0, decimal? RbQty08 = 0, decimal? RbQty09 = 0,
    decimal? RbQty10 = 0, decimal? RbQty11 = 0, decimal? RbQty12 = 0
);
