using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Stock;

[ApiController]
[Route("api/stock/sales-budget")]
[Authorize]
public sealed class StockSalesBudgetController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public StockSalesBudgetController(ApplicationDbContext db) => _db = db;

    // ── DTOs ─────────────────────────────────────────────────────────────────
    public sealed record StockSalesBudgetDto(
        int      Year,
        int      Month,
        string   ItemCode,
        string?  ItemDesc,
        decimal? BudgetedUnits,
        decimal? BudgetedSales,
        decimal? BudgetedDiscount,
        decimal? BudgetedMargin,
        decimal? BudgetedGross,
        decimal? BudgetedCost
    );

    // ── GET api/stock/sales-budget ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? item,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var q = _db.StockSalesBudgets.AsNoTracking();
        if (year.HasValue)   q = q.Where(x => x.SsbYear  == year.Value);
        if (month.HasValue)  q = q.Where(x => x.SsbMonth == month.Value);
        if (!string.IsNullOrWhiteSpace(item))
            q = q.Where(x => x.SsbItemCode.Contains(item) ||
                              (x.SsbItemDesc ?? "").Contains(item));

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.SsbYear)
                           .ThenBy(x => x.SsbMonth)
                           .ThenBy(x => x.SsbItemCode)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync();

        return Ok(PagedResponse<StockSalesBudget>.Ok(items, page, pageSize, total));
    }

    // ── GET api/stock/sales-budget/{id} ───────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.StockSalesBudgets.AsNoTracking()
                            .FirstOrDefaultAsync(x => x.SsbId == id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Not found."));
        return Ok(ApiResponse<StockSalesBudget>.Ok(item));
    }

    // ── POST api/stock/sales-budget ───────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StockSalesBudgetDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ItemCode))
            return BadRequest(ApiResponse.Fail("ItemCode is required."));

        var entity = MapToEntity(new StockSalesBudget(), dto);
        _db.StockSalesBudgets.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<StockSalesBudget>.Ok(entity, "Created."));
    }

    // ── PUT api/stock/sales-budget/{id} ───────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StockSalesBudgetDto dto)
    {
        var entity = await _db.StockSalesBudgets.FindAsync(id);
        if (entity is null) return NotFound(ApiResponse.Fail("Not found."));
        MapToEntity(entity, dto);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<StockSalesBudget>.Ok(entity, "Updated."));
    }

    // ── DELETE api/stock/sales-budget/{id} ────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.StockSalesBudgets.FindAsync(id);
        if (entity is null) return NotFound(ApiResponse.Fail("Not found."));
        _db.StockSalesBudgets.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Deleted."));
    }

    // ── POST api/stock/sales-budget/bulk ─────────────────────────────────────
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkImport([FromBody] List<StockSalesBudgetDto> rows)
    {
        int upserted = 0;
        foreach (var dto in rows)
        {
            var existing = await _db.StockSalesBudgets
                .FirstOrDefaultAsync(x =>
                    x.SsbYear == dto.Year &&
                    x.SsbMonth == dto.Month &&
                    x.SsbItemCode == dto.ItemCode);
            if (existing is null)
            {
                existing = new StockSalesBudget();
                _db.StockSalesBudgets.Add(existing);
            }
            MapToEntity(existing, dto);
            upserted++;
        }
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { UpsertedCount = upserted }, $"Upserted {upserted} records."));
    }

    // ── Mapper ────────────────────────────────────────────────────────────────
    private static StockSalesBudget MapToEntity(StockSalesBudget e, StockSalesBudgetDto dto)
    {
        e.SsbYear             = dto.Year;
        e.SsbMonth            = dto.Month;
        e.SsbItemCode         = dto.ItemCode;
        e.SsbItemDesc         = dto.ItemDesc;
        e.SsbBudgetedUnits    = dto.BudgetedUnits ?? 0;
        e.SsbBudgetedSales    = dto.BudgetedSales ?? 0;
        e.SsbBudgetedDiscount = dto.BudgetedDiscount ?? 0;
        e.SsbBudgetedMargin   = dto.BudgetedMargin ?? 0;
        e.SsbBudgetedGross    = dto.BudgetedGross ?? 0;
        e.SsbBudgetedCost     = dto.BudgetedCost ?? 0;
        return e;
    }
}
