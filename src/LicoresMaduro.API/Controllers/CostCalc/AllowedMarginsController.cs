using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/allowed-margins")]
[Authorize]
[Produces("application/json")]
public sealed class AllowedMarginsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AllowedMarginsController(ApplicationDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var q = _db.CcAllowedMargins.AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => (x.AmItemCode != null && x.AmItemCode.Contains(search))
                           || (x.AmCommodity != null && x.AmCommodity.Contains(search))
                           || (x.AmDescription != null && x.AmDescription.Contains(search)));
        var data = await q.OrderBy(x => x.AmItemCode ?? x.AmCommodity).ToListAsync(ct);
        return Ok(ApiResponse<List<CcAllowedMargin>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _db.CcAllowedMargins.AsNoTracking().FirstOrDefaultAsync(x => x.AmId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Allowed margin {id} not found."));
        return Ok(ApiResponse<CcAllowedMargin>.Ok(item));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] AllowedMarginDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.ItemCode) && string.IsNullOrWhiteSpace(dto.Commodity))
            return BadRequest(ApiResponse.Fail("Either ItemCode or Commodity is required."));
        var item = new CcAllowedMargin
        {
            AmItemCode    = dto.ItemCode?.Trim() ?? null,
            AmCommodity   = dto.Commodity?.Trim() ?? null,
            AmDescription = dto.Description,
            AmMinMargin   = dto.MinMargin,
            AmMaxMargin   = dto.MaxMargin,
            AmDefMargin   = dto.DefMargin,
            IsActive      = dto.IsActive ?? true,
            CreatedAt     = DateTime.UtcNow
        };
        _db.CcAllowedMargins.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.AmId }, ApiResponse<CcAllowedMargin>.Ok(item, "Created."));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] AllowedMarginDto dto, CancellationToken ct)
    {
        var item = await _db.CcAllowedMargins.FirstOrDefaultAsync(x => x.AmId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Allowed margin {id} not found."));
        item.AmItemCode    = dto.ItemCode?.Trim() ?? null;
        item.AmCommodity   = dto.Commodity?.Trim() ?? null;
        item.AmDescription = dto.Description;
        item.AmMinMargin   = dto.MinMargin;
        item.AmMaxMargin   = dto.MaxMargin;
        item.AmDefMargin   = dto.DefMargin;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<CcAllowedMargin>.Ok(item, "Updated."));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await _db.CcAllowedMargins.FirstOrDefaultAsync(x => x.AmId == id, ct);
        if (item is null) return NotFound(ApiResponse.Fail($"Allowed margin {id} not found."));
        _db.CcAllowedMargins.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok("Deleted."));
    }
}

public record AllowedMarginDto(
    string? ItemCode, string? Commodity, string? Description,
    decimal MinMargin, decimal MaxMargin, decimal DefMargin, bool? IsActive);
