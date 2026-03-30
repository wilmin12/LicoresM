using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.RouteAssignment;

[ApiController]
[Route("api/route")]
[Authorize]
[Produces("application/json")]
public sealed class RouteReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly DhwDbContext         _dhw;
    private readonly ILogger<RouteReportsController> _logger;

    public RouteReportsController(
        ApplicationDbContext db,
        DhwDbContext dhw,
        ILogger<RouteReportsController> logger)
    { _db = db; _dhw = dhw; _logger = logger; }

    // ──────────────────────────────────────────────────────────────────────────
    // GET api/route/reports/measures
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Aggregate DAILYT within the date range, grouped by the selected dimension.
    /// dateFrom/dateTo are expected as yyyy-MM-dd strings; internally stored as YYYYMMDD int.
    /// </summary>
    [HttpGet("reports/measures")]
    public async Task<IActionResult> GetMeasures(
        [FromQuery] string dateFrom,
        [FromQuery] string dateTo,
        [FromQuery] string groupBy    = "route",   // route | salesman | customer | brand
        [FromQuery] string? warehouse = null,
        CancellationToken ct = default)
    {
        if (!TryParseDate(dateFrom, out int dtFrom) || !TryParseDate(dateTo, out int dtTo))
            return BadRequest(ApiResponse.Fail("Invalid date format. Use yyyy-MM-dd."));

        var q = _dhw.DailyT.AsNoTracking()
            .Where(x => x.DlyInDt >= dtFrom && x.DlyInDt <= dtTo);

        if (!string.IsNullOrWhiteSpace(warehouse))
            q = q.Where(x => x.DlyWhse == warehouse);

        // Project to anonymous before grouping to avoid translation issues
        var rows = await q.Select(x => new
        {
            x.DlyWhse,
            x.DlyInNo,
            x.DlyAcct,
            x.DlyRoute,
            x.DlySalesRepNo,
            x.DlySalesRepName,
            x.DlyItem,
            x.DlyIdsc,
            x.DlyCqty,
            x.DlyExtp,
            x.DlyDisc,
            x.DlyCcst,
        }).ToListAsync(ct);

        // Group in memory
        IEnumerable<MeasuresRow> result = groupBy.ToLower() switch
        {
            "salesman" => rows
                .GroupBy(x => new { Code = x.DlySalesRepNo ?? "", Name = x.DlySalesRepName ?? "" })
                .Select(g => BuildMeasuresRow(g.Key.Code, g.Key.Name, g)),
            "customer" => rows
                .GroupBy(x => new { Code = x.DlyAcct ?? "", Name = x.DlyAcct ?? "" })
                .Select(g => BuildMeasuresRow(g.Key.Code, g.Key.Name, g)),
            "brand" => rows
                .GroupBy(x => new { Code = ExtractBrand(x.DlyItem ?? ""), Name = ExtractBrand(x.DlyItem ?? "") })
                .Select(g => BuildMeasuresRow(g.Key.Code, g.Key.Name, g)),
            _ => // route (default)
                rows
                .GroupBy(x => new { Code = x.DlyRoute ?? "", Name = x.DlyRoute ?? "" })
                .Select(g => BuildMeasuresRow(g.Key.Code, g.Key.Name, g)),
        };

        return Ok(ApiResponse<IEnumerable<MeasuresRow>>.Ok(result.OrderByDescending(r => r.TotalNettSales)));
    }

    private static MeasuresRow BuildMeasuresRow(
        string groupCode,
        string groupName,
        IEnumerable<dynamic> rows)
    {
        var list = rows.ToList();
        var distinctInvoices  = list.Select(x => x.DlyInNo).Distinct().Count();
        var distinctCustomers = list.Select(x => x.DlyAcct).Distinct().Count();
        var distinctDays      = list.Select(x => x.DlyInDt).Distinct().Count();

        decimal totalCases   = list.Sum(x => (decimal)(x.DlyCqty  ?? 0m));
        decimal totalSales   = list.Sum(x => (decimal)(x.DlyExtp  ?? 0m));
        decimal totalDisc    = list.Sum(x => (decimal)(x.DlyDisc  ?? 0m));
        decimal totalCost    = list.Sum(x => (decimal)(x.DlyCcst  ?? 0m));

        decimal marginPct    = totalSales > 0 ? (totalSales - totalCost) / totalSales * 100m : 0m;
        decimal commission   = totalSales * 0.24m * 0.067m;
        decimal avgCases     = distinctDays > 0 ? totalCases / distinctDays : 0m;
        decimal avgSales     = distinctDays > 0 ? totalSales / distinctDays : 0m;
        decimal avgDisc      = distinctDays > 0 ? totalDisc  / distinctDays : 0m;

        return new MeasuresRow(
            groupCode, groupName,
            totalCases, avgCases,
            totalSales, avgSales,
            totalDisc,  avgDisc,
            distinctInvoices, distinctDays > 0 ? (decimal)distinctInvoices / distinctDays : 0,
            distinctDays, distinctDays > 0 ? 1m : 0m,
            distinctCustomers,
            Math.Round(marginPct, 2),
            Math.Round(commission, 2));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET api/route/reports/visit-schedule
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("reports/visit-schedule")]
    public async Task<IActionResult> GetVisitSchedule(
        [FromQuery] string dateFrom,
        [FromQuery] string dateTo,
        [FromQuery] string? salesmanCode = null,
        [FromQuery] string? route        = null,
        CancellationToken ct = default)
    {
        if (!TryParseDate(dateFrom, out int dtFrom) || !TryParseDate(dateTo, out int dtTo))
            return BadRequest(ApiResponse.Fail("Invalid date format. Use yyyy-MM-dd."));

        // Actual visits: distinct customer+date combinations from DAILYT
        var actualQ = _dhw.DailyT.AsNoTracking()
            .Where(x => x.DlyInDt >= dtFrom && x.DlyInDt <= dtTo);
        if (!string.IsNullOrWhiteSpace(salesmanCode))
            actualQ = actualQ.Where(x => x.DlySalesRepNo == salesmanCode);
        if (!string.IsNullOrWhiteSpace(route))
            actualQ = actualQ.Where(x => x.DlyRoute == route);

        var actualVisits = await actualQ
            .Select(x => new { x.DlyAcct, x.DlyInDt })
            .Distinct()
            .ToListAsync(ct);

        // Customer dimension
        var accounts = actualVisits.Select(x => x.DlyAcct).Distinct().ToList();
        var brattQ   = _dhw.BrattT.AsNoTracking()
            .Where(x => accounts.Contains(x.BrAcct));
        if (!string.IsNullOrWhiteSpace(route))
            brattQ = brattQ.Where(x => x.BrRoute == route);
        var customers = await brattQ.ToListAsync(ct);

        var result = customers.Select(c =>
        {
            var actualDates  = actualVisits
                .Where(v => v.DlyAcct == c.BrAcct)
                .Select(v => IntToWeekday(v.DlyInDt))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            bool hasVariance = actualDates.Count > 0 &&
                !string.IsNullOrWhiteSpace(c.BrVisitDaySalesman) &&
                !actualDates.Any(d => string.Equals(d, c.BrVisitDaySalesman, StringComparison.OrdinalIgnoreCase));

            return new VisitScheduleRow(
                c.BrAcct,
                c.BrName,
                c.BrSalesmanCode,
                c.BrSalesmanName,
                c.BrVisitDaySalesman,
                actualDates,
                hasVariance);
        }).OrderBy(x => x.AccountName).ToList();

        return Ok(ApiResponse<List<VisitScheduleRow>>.Ok(result));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET api/route/reports/delivery-schedule
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("reports/delivery-schedule")]
    public async Task<IActionResult> GetDeliverySchedule(
        [FromQuery] string dateFrom,
        [FromQuery] string dateTo,
        [FromQuery] string? driverCode = null,
        [FromQuery] string? route      = null,
        CancellationToken ct = default)
    {
        if (!TryParseDate(dateFrom, out int dtFrom) || !TryParseDate(dateTo, out int dtTo))
            return BadRequest(ApiResponse.Fail("Invalid date format. Use yyyy-MM-dd."));

        var actualQ = _dhw.DailyT.AsNoTracking()
            .Where(x => x.DlyInDt >= dtFrom && x.DlyInDt <= dtTo);
        if (!string.IsNullOrWhiteSpace(driverCode))
            actualQ = actualQ.Where(x => x.DlyDriver == driverCode);
        if (!string.IsNullOrWhiteSpace(route))
            actualQ = actualQ.Where(x => x.DlyRoute == route);

        var actualDeliveries = await actualQ
            .Select(x => new { x.DlyAcct, x.DlyInDt, x.DlyDriver })
            .Distinct()
            .ToListAsync(ct);

        var accounts = actualDeliveries.Select(x => x.DlyAcct).Distinct().ToList();
        var brattQ   = _dhw.BrattT.AsNoTracking()
            .Where(x => accounts.Contains(x.BrAcct));
        if (!string.IsNullOrWhiteSpace(route))
            brattQ = brattQ.Where(x => x.BrRoute == route);
        var customers = await brattQ.ToListAsync(ct);

        var result = customers.Select(c =>
        {
            var actualDates = actualDeliveries
                .Where(v => v.DlyAcct == c.BrAcct)
                .Select(v => IntToWeekday(v.DlyInDt))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            bool hasVariance = actualDates.Count > 0 &&
                !string.IsNullOrWhiteSpace(c.BrDeliveryDayDriver) &&
                !actualDates.Any(d => string.Equals(d, c.BrDeliveryDayDriver, StringComparison.OrdinalIgnoreCase));

            return new DeliveryScheduleRow(
                c.BrAcct,
                c.BrName,
                c.BrDriverCode,
                c.BrDriverName,
                c.BrDeliveryDayDriver,
                actualDates,
                hasVariance);
        }).OrderBy(x => x.AccountName).ToList();

        return Ok(ApiResponse<List<DeliveryScheduleRow>>.Ok(result));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET api/route/reports/zero-sales
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("reports/zero-sales")]
    public async Task<IActionResult> GetZeroSales(
        [FromQuery] string dateFrom,
        [FromQuery] string dateTo,
        [FromQuery] string? salesmanCode = null,
        [FromQuery] string? route        = null,
        CancellationToken ct = default)
    {
        if (!TryParseDate(dateFrom, out int dtFrom) || !TryParseDate(dateTo, out int dtTo))
            return BadRequest(ApiResponse.Fail("Invalid date format. Use yyyy-MM-dd."));

        // Prior 90 days for "active brands" baseline
        var baselineEnd   = dtFrom;
        var baselineStart = DateToInt(IntToDate(dtFrom).AddDays(-90));

        // Sales in the analysis period
        var periodQ = _dhw.DailyT.AsNoTracking()
            .Where(x => x.DlyInDt >= dtFrom && x.DlyInDt <= dtTo);
        if (!string.IsNullOrWhiteSpace(salesmanCode))
            periodQ = periodQ.Where(x => x.DlySalesRepNo == salesmanCode);
        if (!string.IsNullOrWhiteSpace(route))
            periodQ = periodQ.Where(x => x.DlyRoute == route);

        var periodSales = await periodQ
            .Select(x => new { x.DlySalesRepNo, x.DlySalesRepName, x.DlyAcct, x.DlyItem })
            .Distinct()
            .ToListAsync(ct);

        // Baseline: brands sold per salesman+customer in prior 90 days
        var baselineQ = _dhw.DailyT.AsNoTracking()
            .Where(x => x.DlyInDt >= baselineStart && x.DlyInDt < baselineEnd);
        if (!string.IsNullOrWhiteSpace(salesmanCode))
            baselineQ = baselineQ.Where(x => x.DlySalesRepNo == salesmanCode);
        if (!string.IsNullOrWhiteSpace(route))
            baselineQ = baselineQ.Where(x => x.DlyRoute == route);

        var baseline = await baselineQ
            .Select(x => new { x.DlySalesRepNo, x.DlyAcct, x.DlyItem })
            .Distinct()
            .ToListAsync(ct);

        // Build zero-sales: brands in baseline not appearing in period for same salesman+customer
        var periodSet = periodSales
            .Select(x => (x.DlySalesRepNo, x.DlyAcct, x.DlyItem))
            .ToHashSet();

        var zeroSales = baseline
            .Where(b => !periodSet.Contains((b.DlySalesRepNo, b.DlyAcct, b.DlyItem)))
            .GroupBy(b => new { b.DlySalesRepNo, b.DlyAcct })
            .Select(g => new ZeroSalesRow(
                g.Key.DlySalesRepNo,
                g.Key.DlyAcct,
                g.Select(b => b.DlyItem ?? "").Distinct().OrderBy(i => i).ToList()))
            .OrderBy(x => x.SalesRepNo)
            .ThenBy(x => x.AccountNumber)
            .ToList();

        return Ok(ApiResponse<List<ZeroSalesRow>>.Ok(zeroSales));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET api/route/reports/commission
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("reports/commission")]
    public async Task<IActionResult> GetCommission(
        [FromQuery] string dateFrom,
        [FromQuery] string dateTo,
        [FromQuery] string? salesmanCode = null,
        CancellationToken ct = default)
    {
        if (!TryParseDate(dateFrom, out int dtFrom) || !TryParseDate(dateTo, out int dtTo))
            return BadRequest(ApiResponse.Fail("Invalid date format. Use yyyy-MM-dd."));

        var q = _dhw.DailyT.AsNoTracking()
            .Where(x => x.DlyInDt >= dtFrom && x.DlyInDt <= dtTo);
        if (!string.IsNullOrWhiteSpace(salesmanCode))
            q = q.Where(x => x.DlySalesRepNo == salesmanCode);

        var rows = await q
            .Select(x => new { x.DlySalesRepNo, x.DlySalesRepName, x.DlyExtp, x.DlyItem })
            .ToListAsync(ct);

        // ExPolar: exclude brands containing "POLAR" (based on item code convention)
        var result = rows
            .Where(x => !IsPolar(x.DlyItem))
            .GroupBy(x => new { Code = x.DlySalesRepNo ?? "", Name = x.DlySalesRepName ?? "" })
            .Select(g =>
            {
                decimal nettSales  = g.Sum(x => x.DlyExtp ?? 0m);
                decimal commission = nettSales * 0.24m * 0.067m;
                return new CommissionRow(g.Key.Code, g.Key.Name, Math.Round(nettSales, 2), Math.Round(commission, 2));
            })
            .OrderByDescending(x => x.CurrentPeriodNettSales)
            .ToList();

        return Ok(ApiResponse<List<CommissionRow>>.Ok(result));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET api/route/dimensions/customers
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("dimensions/customers")]
    public async Task<IActionResult> GetCustomerDimensions(
        [FromQuery] string? search   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 50,
        CancellationToken ct = default)
    {
        var brattQ = _dhw.BrattT.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            brattQ = brattQ.Where(x => x.BrAcct.Contains(search) || (x.BrName != null && x.BrName.Contains(search)));

        var total    = await brattQ.CountAsync(ct);
        var customers = await brattQ.OrderBy(x => x.BrAcct)
                                     .Skip((page - 1) * pageSize).Take(pageSize)
                                     .ToListAsync(ct);

        var accounts = customers.Select(c => c.BrAcct).ToList();
        var exts     = await _db.RouteCustomerExts.AsNoTracking()
            .Where(x => accounts.Contains(x.RceAccountNumber))
            .ToListAsync(ct);
        var extDict = exts.ToDictionary(x => x.RceAccountNumber);

        var result = customers.Select(c => new CustomerDimRow(c, extDict.GetValueOrDefault(c.BrAcct))).ToList();
        return Ok(PagedResponse<CustomerDimRow>.Ok(result, page, pageSize, total));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET api/route/dimensions/products
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("dimensions/products")]
    public async Task<IActionResult> GetProductDimensions(
        [FromQuery] string? search   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 50,
        CancellationToken ct = default)
    {
        var itemQ = _dhw.ItemT.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            itemQ = itemQ.Where(x => x.ItItem.Contains(search) || (x.ItDesc != null && x.ItDesc.Contains(search)));

        var total  = await itemQ.CountAsync(ct);
        var items  = await itemQ.OrderBy(x => x.ItItem)
                                 .Skip((page - 1) * pageSize).Take(pageSize)
                                 .ToListAsync(ct);

        var itemCodes = items.Select(i => i.ItItem).ToList();
        var exts      = await _db.RouteProductExts.AsNoTracking()
            .Where(x => itemCodes.Contains(x.RpeItemCode))
            .ToListAsync(ct);
        var extDict = exts.ToDictionary(x => x.RpeItemCode);

        var result = items.Select(i => new ProductDimRow(i, extDict.GetValueOrDefault(i.ItItem))).ToList();
        return Ok(PagedResponse<ProductDimRow>.Ok(result, page, pageSize, total));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────
    private static bool TryParseDate(string? s, out int yyyymmdd)
    {
        yyyymmdd = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (!DateOnly.TryParse(s, out var d)) return false;
        yyyymmdd = d.Year * 10000 + d.Month * 100 + d.Day;
        return true;
    }

    private static DateOnly IntToDate(int yyyymmdd)
    {
        int y = yyyymmdd / 10000;
        int m = (yyyymmdd / 100) % 100;
        int d = yyyymmdd % 100;
        return new DateOnly(y, m, d);
    }

    private static int DateToInt(DateOnly d) => d.Year * 10000 + d.Month * 100 + d.Day;

    private static string IntToWeekday(int yyyymmdd)
    {
        var d = IntToDate(yyyymmdd);
        return d.DayOfWeek.ToString();
    }

    private static string ExtractBrand(string item) =>
        item.Length >= 4 ? item[..4].ToUpperInvariant() : item.ToUpperInvariant();

    private static bool IsPolar(string? item) =>
        item is not null && item.Contains("POLAR", StringComparison.OrdinalIgnoreCase);
}

// ──────────────────────────────────────────────────────────────────────────────
// Response DTOs
// ──────────────────────────────────────────────────────────────────────────────

public sealed record MeasuresRow(
    string  GroupCode,
    string  GroupName,
    decimal TotalCases,
    decimal AvgCases,
    decimal TotalNettSales,
    decimal AvgNettSales,
    decimal TotalDiscounts,
    decimal AvgDiscounts,
    int     TotalInvoices,
    decimal AvgInvoices,
    int     TotalStops,
    decimal AvgStops,
    int     TotalCustomers,
    decimal MarginPct,
    decimal Commission
);

public sealed record VisitScheduleRow(
    string?       AccountNumber,
    string?       AccountName,
    string?       SalesmanCode,
    string?       SalesmanName,
    string?       ScheduledVisitDay,
    List<string>  ActualVisitDays,
    bool          HasVariance
);

public sealed record DeliveryScheduleRow(
    string?       AccountNumber,
    string?       AccountName,
    string?       DriverCode,
    string?       DriverName,
    string?       ScheduledDeliveryDay,
    List<string>  ActualDeliveryDays,
    bool          HasVariance
);

public sealed record ZeroSalesRow(
    string?      SalesRepNo,
    string?      AccountNumber,
    List<string> BrandsWithZeroSales
);

public sealed record CommissionRow(
    string  SalesRepNo,
    string  SalesRepName,
    decimal CurrentPeriodNettSales,
    decimal CurrentCommission
);

public sealed record CustomerDimRow(
    // BRATTT fields
    string?  AccountNumber,
    string?  AccountName,
    string?  Address,
    string?  Status,
    string?  Route,
    string?  RouteDesc,
    string?  SalesmanCode,
    string?  SalesmanName,
    string?  DriverCode,
    string?  DriverName,
    string?  SubClass,
    string?  OnOffPremise,
    string?  RetailClass,
    string?  VisitDaySalesman,
    string?  DeliveryDayDriver,
    // ROUTE_CUSTOMER_EXT fields
    int?     RceId,
    string?  RceRouteNpActive,
    string?  RceRouteOvd5,
    string?  RceRouteOvd6,
    string?  RcePareto1Overall,
    string?  RcePareto2Overall,
    bool     RceCoolerPolar,
    bool     RceCoolerCorona,
    bool     RceHighTraffic
)
{
    public CustomerDimRow(DhwBrattT c, RouteCustomerExt? e) : this(
        c.BrAcct, c.BrName, c.BrAddress, c.BrStatus,
        c.BrRoute, c.BrRouteDesc, c.BrSalesmanCode, c.BrSalesmanName,
        c.BrDriverCode, c.BrDriverName, c.BrSubClass, c.BrOnOffPremise,
        c.BrRetailClass, c.BrVisitDaySalesman, c.BrDeliveryDayDriver,
        e?.RceId, e?.RceRouteNpActive, e?.RceRouteOvd5, e?.RceRouteOvd6,
        e?.RcePareto1Overall, e?.RcePareto2Overall,
        e?.RceCoolerPolar ?? false, e?.RceCoolerCorona ?? false, e?.RceHighTraffic ?? false)
    { }
}

public sealed record ProductDimRow(
    // ITEMT fields
    string?  ItemCode,
    string?  ItemDesc,
    string?  Status,
    string?  BrandCode,
    string?  BrandDesc,
    string?  SubCode,
    string?  SubDesc,
    string?  ProductClass,
    string?  ProductClassDesc,
    int?     UnitsPerCase,
    int?     MlPerBottle,
    // ROUTE_PRODUCT_EXT fields
    int?     RpeId,
    string?  RpeGroupCodeBeerWaterOthers,
    string?  RpeGroupCodeBrandSpecific
)
{
    public ProductDimRow(DhwItemT i, RouteProductExt? e) : this(
        i.ItItem, i.ItDesc, i.ItStatus,
        i.ItBrandCode, null, null, null,
        i.ItProductClass, null, i.ItUnitsPerCase, i.ItMlPerBottle,
        e?.RpeId, e?.RpeGroupCodeBeerWaterOthers, e?.RpeGroupCodeBrandSpecific)
    { }
}
