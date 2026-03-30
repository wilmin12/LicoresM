using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.RouteAssignment;

[ApiController]
[Route("api/route/product-ext")]
[Authorize]
[Produces("application/json")]
public sealed class RouteProductExtController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RouteProductExtController> _logger;

    public RouteProductExtController(ApplicationDbContext db, ILogger<RouteProductExtController> logger)
    { _db = db; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search   = null,
        [FromQuery] int    page      = 1,
        [FromQuery] int    pageSize  = 50,
        CancellationToken ct = default)
    {
        var q = _db.RouteProductExts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.RpeItemCode.Contains(search));
        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(x => x.RpeItemCode)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return Ok(PagedResponse<RouteProductExt>.Ok(data, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.RouteProductExts.FindAsync([id], ct);
        return e is null
            ? NotFound(ApiResponse.Fail($"Product ext record {id} not found."))
            : Ok(ApiResponse<RouteProductExt>.Ok(e));
    }

    /// <summary>Upsert by ItemCode — insert if not found, update if exists.</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Upsert([FromBody] RouteProductExtDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

        var existing = await _db.RouteProductExts
            .FirstOrDefaultAsync(x => x.RpeItemCode == dto.RpeItemCode, ct);

        if (existing is null)
        {
            existing = new RouteProductExt();
            _db.RouteProductExts.Add(existing);
        }

        existing.RpeItemCode                 = dto.RpeItemCode;
        existing.RpeGroupCodeBeerWaterOthers = dto.RpeGroupCodeBeerWaterOthers;
        existing.RpeGroupCodeBrandSpecific   = dto.RpeGroupCodeBrandSpecific;
        existing.UpdatedAt                   = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("RouteProductExt upserted for item '{Item}'", dto.RpeItemCode);
        return Ok(ApiResponse<RouteProductExt>.Ok(existing, "Saved."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await _db.RouteProductExts.FindAsync([id], ct);
        if (e is null) return NotFound(ApiResponse.Fail($"Product ext record {id} not found."));
        _db.RouteProductExts.Remove(e);
        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("RouteProductExt {Id} deleted", id);
        return Ok(ApiResponse.Ok("Record deleted."));
    }
}

public sealed record RouteProductExtDto(
    string  RpeItemCode,
    string? RpeGroupCodeBeerWaterOthers,
    string? RpeGroupCodeBrandSpecific
);
