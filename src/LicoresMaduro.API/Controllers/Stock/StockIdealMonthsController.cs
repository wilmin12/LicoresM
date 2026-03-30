using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Stock;

[ApiController]
[Route("api/stock/ideal-months")]
[Authorize]
public sealed class StockIdealMonthsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public StockIdealMonthsController(ApplicationDbContext db) => _db = db;

    // ── DTOs ─────────────────────────────────────────────────────────────────
    public sealed record StockIdealMonthsDto(
        string   ItemCode,
        decimal  IdealMonths,
        string?  OrderFreq,
        DateOnly? StockStartDate
    );

    // ── GET api/stock/ideal-months ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var q = _db.StockIdealMonths.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.SimItemCode.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.SimItemCode)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync();

        return Ok(PagedResponse<StockIdealMonths>.Ok(items, page, pageSize, total));
    }

    // ── GET api/stock/ideal-months/{id} ───────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.StockIdealMonths.AsNoTracking()
                            .FirstOrDefaultAsync(x => x.SimId == id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        return Ok(ApiResponse<StockIdealMonths>.Ok(item));
    }

    // ── PUT api/stock/ideal-months (upsert by ItemCode) ───────────────────────
    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] StockIdealMonthsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ItemCode))
            return BadRequest(ApiResponse.Fail("ItemCode is required."));

        var existing = await _db.StockIdealMonths
                                .FirstOrDefaultAsync(x => x.SimItemCode == dto.ItemCode);
        if (existing is null)
        {
            existing = new StockIdealMonths { SimItemCode = dto.ItemCode };
            _db.StockIdealMonths.Add(existing);
        }

        existing.SimIdealMonths     = dto.IdealMonths;
        existing.SimOrderFreq       = dto.OrderFreq;
        existing.SimStockStartDate  = dto.StockStartDate;
        existing.UpdatedAt          = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<StockIdealMonths>.Ok(existing, "Saved."));
    }

    // ── DELETE api/stock/ideal-months/{id} ───────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.StockIdealMonths.FindAsync(id);
        if (item is null) return NotFound(ApiResponse.Fail("Not found."));
        _db.StockIdealMonths.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Deleted."));
    }
}
