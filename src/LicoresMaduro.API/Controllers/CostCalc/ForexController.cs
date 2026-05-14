using LicoresMaduro.API.Data;
using LicoresMaduro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.CostCalc;

[ApiController]
[Route("api/cost-calc/forex")]
[Authorize]
public class ForexController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IForexScraperService _scraper;

    public ForexController(ApplicationDbContext db, IForexScraperService scraper)
    {
        _db = db;
        _scraper = scraper;
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencies(CancellationToken ct)
    {
        var currencies = await _db.CcForexHistories
            .Select(x => x.FxCurrency)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
        return Ok(currencies);
    }

    [HttpGet("rate/{currency}/{date}")]
    public async Task<IActionResult> GetRateByDate(string currency, string date, CancellationToken ct)
    {
        var code = currency.Trim().ToUpperInvariant();
        if (!DateTime.TryParse(date, out var parsedDate))
            return BadRequest(new { message = "Invalid date format." });

        // Exact date first, then nearest previous date
        var rate = await _db.CcForexHistories
            .Where(x => x.FxCurrency == code && x.FxDate <= parsedDate.Date)
            .OrderByDescending(x => x.FxDate)
            .Select(x => new { x.FxSellingRate, x.FxDate })
            .FirstOrDefaultAsync(ct);

        if (rate == null)
            return NotFound(new { message = $"No forex rate found for '{code}' on or before {date}." });

        return Ok(new { currency = code, sellingRate = rate.FxSellingRate, rateDate = rate.FxDate });
    }

    [HttpGet("rate/{currency}")]
    public async Task<IActionResult> GetLatestRate(string currency, CancellationToken ct)
    {
        var code = currency.Trim().ToUpperInvariant();
        var rate = await _db.CcForexHistories
            .Where(x => x.FxCurrency == code)
            .OrderByDescending(x => x.FxDate)
            .Select(x => x.FxSellingRate)
            .FirstOrDefaultAsync(ct);

        if (rate == null)
            return NotFound(new { message = $"No forex rate found for currency '{code}'." });

        return Ok(new { currency = code, sellingRate = rate });
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestRates(CancellationToken ct)
    {
        // To avoid EF Core translation errors with GroupBy, we can do it in memory since the list of currencies is small.
        var data = await _db.CcForexHistories
            .OrderByDescending(x => x.FxDate)
            .ToListAsync(ct);

        var latestRates = data
            .GroupBy(x => x.FxCurrency)
            .Select(g => g.First())
            .Select(x => new { Currency = x.FxCurrency, SellingRate = x.FxSellingRate, Date = x.FxDate })
            .ToList();

        return Ok(latestRates);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int days = 30)
    {
        var cutoff = DateTime.Today.AddDays(-days);
        var data = await _db.CcForexHistories
            .Where(x => x.FxDate >= cutoff)
            .OrderByDescending(x => x.FxDate)
            .ThenBy(x => x.FxCurrency)
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncManual()
    {
        try
        {
            int count = await _scraper.ScrapeAndSaveRatesAsync();
            return Ok(new { Message = $"Sync completed. {count} new rates saved.", Count = count });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? "";
            return StatusCode(500, new { Error = ex.Message, Detail = inner });
        }
    }
}
