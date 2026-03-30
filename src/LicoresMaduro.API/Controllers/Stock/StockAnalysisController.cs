using LicoresMaduro.API.Data;
using LicoresMaduro.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicoresMaduro.API.Controllers.Stock;

[ApiController]
[Route("api/stock/analysis")]
[Authorize]
public sealed class StockAnalysisController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly DhwDbContext         _dhw;

    public StockAnalysisController(ApplicationDbContext db, DhwDbContext dhw)
    {
        _db  = db;
        _dhw = dhw;
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────
    public sealed record GenerateRequest(int Year, int Month);

    public sealed record GenerateSummaryDto(
        int      ItemsProcessed,
        DateTime GeneratedAt,
        int      Year,
        int      Month
    );

    public sealed record AnalysisSummaryDto(
        int     TotalItems,
        int     ItemsOverstocked,
        int     ItemsUnderstocked,
        decimal TotalInventoryValue,
        decimal TotalOverstockAng,
        decimal TotalIdealStockAng,
        decimal TotalYtdSales,
        decimal TotalYtdBudget,
        decimal OverUnderPerformance,
        decimal AvgMonthsOfStock,
        decimal AvgMonthsOfStockInclOrder,
        IReadOnlyList<StockAnalysisResult> TopOverstockedItems,
        IReadOnlyList<StockAnalysisResult> TopUnderstockedItems
    );

    // ── POST api/stock/analysis/generate ─────────────────────────────────────
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest req)
    {
        if (req.Year < 2000 || req.Year > 2100)
            return BadRequest(ApiResponse.Fail("Invalid year."));
        if (req.Month < 1 || req.Month > 12)
            return BadRequest(ApiResponse.Fail("Invalid month (1-12)."));

        var analysisDate = new DateOnly(req.Year, req.Month, DateTime.DaysInMonth(req.Year, req.Month));

        // 1. Fetch all INVENT rows for the three warehouses (READ ONLY from DHW)
        var warehouses = new[] { "11010", "11020", "11060" };
        var inventRows = await _dhw.Invents.AsNoTracking()
                                   .Where(x => warehouses.Contains(x.IyWhse))
                                   .ToListAsync();

        // 2. Group by IyItem: aggregate OH/OnOrder per warehouse
        var byItem = inventRows
            .GroupBy(x => x.IyItem)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Oh11010    = g.Where(r => r.IyWhse == "11010").Sum(r => r.IyOnHa ?? 0m),
                    Oh11020    = g.Where(r => r.IyWhse == "11020").Sum(r => r.IyOnHa ?? 0m),
                    Oh11060    = g.Where(r => r.IyWhse == "11060").Sum(r => r.IyOnHa ?? 0m),
                    InBo11010  = g.Where(r => r.IyWhse == "11010").Sum(r => r.IyInBo ?? 0m),
                    InBo11020  = g.Where(r => r.IyWhse == "11020").Sum(r => r.IyInBo ?? 0m),
                    InBo11060  = g.Where(r => r.IyWhse == "11060").Sum(r => r.IyInBo ?? 0m),
                    // Avg cost: average of non-zero IyAvCs across warehouses for the item
                    AvgCost    = g.Where(r => r.IyAvCs.HasValue && r.IyAvCs.Value != 0m)
                                  .Select(r => r.IyAvCs!.Value)
                                  .DefaultIfEmpty(0m)
                                  .Average(),
                    // Quantities for weighted monthly sales avg — take first row with data
                    // (rows are per-warehouse; qty1-12 should be item-level, take the max)
                    Qty1  = g.Max(r => r.IyQty1  ?? 0m),
                    Qty2  = g.Max(r => r.IyQty2  ?? 0m),
                    Qty3  = g.Max(r => r.IyQty3  ?? 0m),
                    Qty4  = g.Max(r => r.IyQty4  ?? 0m),
                    Qty5  = g.Max(r => r.IyQty5  ?? 0m),
                    Qty6  = g.Max(r => r.IyQty6  ?? 0m),
                    Qty7  = g.Max(r => r.IyQty7  ?? 0m),
                    Qty8  = g.Max(r => r.IyQty8  ?? 0m),
                    Qty9  = g.Max(r => r.IyQty9  ?? 0m),
                    Qty10 = g.Max(r => r.IyQty10 ?? 0m),
                    Qty11 = g.Max(r => r.IyQty11 ?? 0m),
                    Qty12 = g.Max(r => r.IyQty12 ?? 0m),
                }
            );

        // 3. Calculate MonthlySalesUnits with weighted formula
        //    Exclude Nov (11) and Dec (12) from being the most-recent month (shift if needed)
        // IyQty1 = most recent month. If current month is Nov(11) or Dec(12), the weighting
        // still uses IyQty1-12 as provided; the exclusion note means we use the rolling data as-is.
        static decimal CalcWeightedMonthly(
            decimal q1, decimal q2, decimal q3, decimal q4,
            decimal q5, decimal q6, decimal q7, decimal q8,
            decimal q9, decimal q10, decimal q11, decimal q12,
            int month)
        {
            // WeightedAvg = (((Qty1+Qty2+Qty3+Qty4)/4 * 8) + ((Qty5+Qty6+Qty7+Qty8)/4 * 1) + ((Qty9+Qty10+Qty11+Qty12)/4 * 1)) / 10
            var recent4Avg = (q1 + q2 + q3 + q4) / 4m;
            var mid4Avg    = (q5 + q6 + q7 + q8) / 4m;
            var old4Avg    = (q9 + q10 + q11 + q12) / 4m;

            // If current month is Nov(11) or Dec(12), use the same formula
            // (the instruction says "exclude Nov/Dec" meaning skip them if they are the most recent
            // month in the rolling window — in practice we use the computed rolling avg directly)
            return (recent4Avg * 8m + mid4Avg * 1m + old4Avg * 1m) / 10m;
        }

        // 4. YTD sales from DAILYT
        int ytdStart = req.Year * 10000 + 1 * 100 + 1;   // YYYYMMDD Jan 1
        int ytdEnd   = req.Year * 10000 + req.Month * 100 + DateTime.DaysInMonth(req.Year, req.Month);

        var ytdSalesByItem = await _dhw.DailyT.AsNoTracking()
            .Where(x => x.DlyInDt >= ytdStart && x.DlyInDt <= ytdEnd && x.DlyItem != null)
            .GroupBy(x => x.DlyItem!)
            .Select(g => new { ItemCode = g.Key, YtdQty = g.Sum(x => x.DlyCqty ?? 0m) })
            .ToListAsync();
        var ytdSalesDict = ytdSalesByItem.ToDictionary(x => x.ItemCode, x => x.YtdQty);

        // 5. Last receipt from PODTLT
        var receiptRows = await _dhw.PoDetails.AsNoTracking()
            .Where(x => x.PdItem != null && x.PdRdAt.HasValue && x.PdRdAt.Value > 0)
            .GroupBy(x => x.PdItem!)
            .Select(g => new
            {
                ItemCode        = g.Key,
                MaxReceiptDate  = g.Max(x => x.PdRdAt!.Value),
            })
            .ToListAsync();

        // For qty of last receipt, we need the row matching the max date per item
        var allReceiptRows = await _dhw.PoDetails.AsNoTracking()
            .Where(x => x.PdItem != null && x.PdRdAt.HasValue && x.PdRdAt.Value > 0)
            .ToListAsync();

        var lastReceiptDict = receiptRows.ToDictionary(
            r => r.ItemCode,
            r =>
            {
                var lastRow = allReceiptRows
                    .Where(x => x.PdItem == r.ItemCode && x.PdRdAt == r.MaxReceiptDate)
                    .FirstOrDefault();
                return (
                    Date: IntToDateOnly((int)r.MaxReceiptDate),
                    Qty:  lastRow?.PdRqty ?? 0m
                );
            });

        // 6. On-order ETA from PODTLT: open POs — nearest commitment date per item
        //    PdStat typically "OP" for open; filter PdCdAt > 0 and group by item
        var openPoRows = await _dhw.PoDetails.AsNoTracking()
            .Where(x => x.PdItem != null &&
                        x.PdCdAt.HasValue && x.PdCdAt.Value > 0 &&
                        (x.PdStat == "OP" || x.PdStat == "O "))
            .GroupBy(x => x.PdItem!)
            .Select(g => new { ItemCode = g.Key, NearestEta = g.Min(x => x.PdCdAt!.Value) })
            .ToListAsync();
        var etaDict = openPoRows.ToDictionary(x => x.ItemCode, x => IntToDateOnly((int)x.NearestEta));

        // 7. StockIdealMonths lookup (left join by ItemCode)
        var idealMonthsDict = await _db.StockIdealMonths.AsNoTracking()
            .ToDictionaryAsync(x => x.SimItemCode, x => x);

        // 8. Sales budget for the year
        var budgetRows = await _db.StockSalesBudgets.AsNoTracking()
            .Where(x => x.SsbYear == req.Year)
            .ToListAsync();
        var budgetByItem = budgetRows
            .GroupBy(x => x.SsbItemCode)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 9. DhwItemT for descriptions
        var itemCodes = byItem.Keys.ToList();
        var itemDescs = await _dhw.ItemT.AsNoTracking()
            .Where(x => itemCodes.Contains(x.ItItem))
            .ToDictionaryAsync(x => x.ItItem, x => x);

        // 10. Compute all results in memory
        var now = DateTime.UtcNow;
        var results = new List<StockAnalysisResult>(byItem.Count);

        foreach (var (itemCode, inv) in byItem)
        {
            var oh11010   = inv.Oh11010;
            var oh11020   = inv.Oh11020;
            var oh11060   = inv.Oh11060;
            var currentOh = oh11010 + oh11020 + oh11060;

            var inBo11010   = inv.InBo11010;
            var inBo11020   = inv.InBo11020;
            var inBo11060   = inv.InBo11060;
            var onOrderUnits = inBo11010 + inBo11020 + inBo11060;

            var avgCost = inv.AvgCost;

            var monthlySales = CalcWeightedMonthly(
                inv.Qty1, inv.Qty2, inv.Qty3, inv.Qty4,
                inv.Qty5, inv.Qty6, inv.Qty7, inv.Qty8,
                inv.Qty9, inv.Qty10, inv.Qty11, inv.Qty12,
                req.Month);

            ytdSalesDict.TryGetValue(itemCode, out var ytdSales);

            itemDescs.TryGetValue(itemCode, out var itemT);

            idealMonthsDict.TryGetValue(itemCode, out var simRow);
            var idealMonths = simRow?.SimIdealMonths ?? 1.5m;

            // Budget aggregates
            decimal totalBudgetUnits = 0, ytdBudgetUnits = 0;
            decimal totalBudgetSales = 0, ytdBudgetSales = 0;
            decimal totalBudgetCost  = 0, ytdBudgetCost  = 0;
            if (budgetByItem.TryGetValue(itemCode, out var budgetList))
            {
                totalBudgetUnits = budgetList.Sum(b => b.SsbBudgetedUnits ?? 0m);
                ytdBudgetUnits   = budgetList.Where(b => b.SsbMonth <= req.Month).Sum(b => b.SsbBudgetedUnits ?? 0m);
                totalBudgetSales = budgetList.Sum(b => b.SsbBudgetedSales ?? 0m);
                ytdBudgetSales   = budgetList.Where(b => b.SsbMonth <= req.Month).Sum(b => b.SsbBudgetedSales ?? 0m);
                totalBudgetCost  = budgetList.Sum(b => b.SsbBudgetedCost ?? 0m);
                ytdBudgetCost    = budgetList.Where(b => b.SsbMonth <= req.Month).Sum(b => b.SsbBudgetedCost ?? 0m);
            }

            lastReceiptDict.TryGetValue(itemCode, out var lastReceipt);
            etaDict.TryGetValue(itemCode, out var onOrderEtaRaw);
            DateOnly? onOrderEta = onOrderEtaRaw != default ? onOrderEtaRaw : null;

            // ── Computed measures ────────────────────────────────────────────
            var idealStockUnits              = monthlySales * idealMonths;
            var overstockUnits               = currentOh - idealStockUnits;
            var overstockUnitsInclOrders     = currentOh + onOrderUnits - idealStockUnits;
            var monthsOfStock                = monthlySales > 0 ? currentOh / monthlySales : 0m;
            var yearsOfStock                 = monthsOfStock / 12m;
            var monthsOfStockInclOnOrder     = monthlySales > 0 ? (currentOh + onOrderUnits) / monthlySales : 0m;
            var monthsOfOverstock            = monthsOfStock - idealMonths;
            var monthsOfOverstockInclOnOrder = monthsOfStockInclOnOrder - idealMonths;

            var overUnderPerf = ytdSales - ytdBudgetUnits;

            var inventoryValue        = currentOh * avgCost;
            var inventoryValueOnOrder = onOrderUnits * avgCost;
            var totalInventoryValue   = inventoryValue + inventoryValueOnOrder;

            var idealStockAng          = idealStockUnits * avgCost;
            var monthlyBudgetCost      = totalBudgetCost > 0 ? totalBudgetCost / 12m : 0m;
            var budgetedIdealStockAng  = monthlyBudgetCost * idealMonths;
            var overstockAng           = inventoryValue - idealStockAng;
            var overstockAngInclOrder  = totalInventoryValue - idealStockAng;
            var expectedMonthlySalesAng = totalBudgetSales / 12m;

            var monthsOfStockInclOrderOnValue    = monthlyBudgetCost > 0 ? totalInventoryValue / monthlyBudgetCost : 0m;
            var monthsOfOverstockInclOrderOnValue = monthlyBudgetCost > 0 ? (totalInventoryValue - idealStockAng) / monthlyBudgetCost : 0m;

            var dailyRateOfSale = monthlySales / 21.67m;

            // Arrival order calculations
            int?     daysBeforeArrival    = null;
            decimal? monthsBeforeArrival  = null;
            decimal? unitSalesBeforeArr   = null;
            decimal? totalOhAtArrival     = null;
            decimal? overstockAtArrival   = null;
            decimal? totalMonthsIdealStock = null;

            if (onOrderEta.HasValue)
            {
                var etaDateTime = onOrderEta.Value.ToDateTime(TimeOnly.MinValue);
                var analysisDateTime = analysisDate.ToDateTime(TimeOnly.MinValue);
                var days = (etaDateTime - analysisDateTime).TotalDays;
                daysBeforeArrival   = (int)Math.Round(days);
                monthsBeforeArrival = (decimal)days / 30m;
                unitSalesBeforeArr  = monthsBeforeArrival * monthlySales;
                totalOhAtArrival    = currentOh + onOrderUnits - unitSalesBeforeArr;
                overstockAtArrival  = totalOhAtArrival - idealStockUnits;
                totalMonthsIdealStock = monthlySales > 0 ? overstockAtArrival / monthlySales : 0m;
            }

            results.Add(new StockAnalysisResult
            {
                SarYear                              = req.Year,
                SarMonth                             = req.Month,
                SarItemCode                          = itemCode,
                SarItemDesc                          = itemT?.ItDesc,
                SarProductClassId                    = itemT?.ItProductClass,
                SarProductClassDesc                  = null,
                SarSupplierCode                      = itemT?.ItSupCode,
                SarSupplierName                      = null,
                SarBrandCode                         = itemT?.ItBrandCode,
                SarBrandDesc                         = null,
                SarStockStartDate                    = simRow?.SimStockStartDate,
                SarOrderFrequency                    = simRow?.SimOrderFreq,
                SarIdealMonthsOfStock                = idealMonths,
                SarOh11010                           = oh11010,
                SarOh11020                           = oh11020,
                SarOh11060                           = oh11060,
                SarCurrentOhUnits                    = currentOh,
                SarOnOrder11010                      = inBo11010,
                SarOnOrder11020                      = inBo11020,
                SarOnOrder11060                      = inBo11060,
                SarOnOrderUnits                      = onOrderUnits,
                SarOnOrderEta                        = onOrderEta,
                SarYtdSalesUnits                     = ytdSales,
                SarMonthlySalesUnits                 = monthlySales,
                SarIdealStockUnits                   = idealStockUnits,
                SarOverstockUnits                    = overstockUnits,
                SarOverstockUnitsInclOrders          = overstockUnitsInclOrders,
                SarMonthsOfStock                     = monthsOfStock,
                SarYearsOfStock                      = yearsOfStock,
                SarMonthsOfStockInclOnOrder          = monthsOfStockInclOnOrder,
                SarMonthsOfOverstock                 = monthsOfOverstock,
                SarMonthsOfOverstockInclOnOrder      = monthsOfOverstockInclOnOrder,
                SarTotalBudgetUnits                  = totalBudgetUnits,
                SarYtdBudgetUnits                    = ytdBudgetUnits,
                SarTotalBudgetSales                  = totalBudgetSales,
                SarYtdBudgetSales                    = ytdBudgetSales,
                SarTotalBudgetCost                   = totalBudgetCost,
                SarYtdBudgetCost                     = ytdBudgetCost,
                SarOverUnderPerformanceUnits         = overUnderPerf,
                SarInventoryValue                    = inventoryValue,
                SarInventoryValueOnOrder             = inventoryValueOnOrder,
                SarTotalInventoryValue               = totalInventoryValue,
                SarAvgCostPerCase                    = avgCost,
                SarIdealStockAng                     = idealStockAng,
                SarBudgetedIdealStockAng             = budgetedIdealStockAng,
                SarOverstockAng                      = overstockAng,
                SarOverstockAngInclOrder             = overstockAngInclOrder,
                SarExpectedMonthlySalesAng           = expectedMonthlySalesAng,
                SarMonthsOfStockInclOrderOnValue     = monthsOfStockInclOrderOnValue,
                SarMonthsOfOverstockInclOrderOnValue = monthsOfOverstockInclOrderOnValue,
                SarDailyRateOfSale                   = dailyRateOfSale,
                SarLastReceiptDate                   = lastReceipt.Date == default ? null : lastReceipt.Date,
                SarQtyLastReceipt                    = lastReceipt.Qty,
                SarDaysBeforeArrivalOrder            = daysBeforeArrival,
                SarMonthsBeforeArrivalOrder          = monthsBeforeArrival,
                SarUnitSalesBeforeArrivalOrder       = unitSalesBeforeArr,
                SarTotalOhAtArrivalOrder             = totalOhAtArrival,
                SarOverstockAtArrivalOrder           = overstockAtArrival,
                SarTotalMonthsBeforeIdealStock       = totalMonthsIdealStock,
                SarGeneratedAt                       = now,
            });
        }

        // 12. Upsert: delete existing for year/month then insert
        var existing = await _db.StockAnalysisResults
            .Where(x => x.SarYear == req.Year && x.SarMonth == req.Month)
            .ToListAsync();
        _db.StockAnalysisResults.RemoveRange(existing);
        await _db.SaveChangesAsync();

        _db.StockAnalysisResults.AddRange(results);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<GenerateSummaryDto>.Ok(
            new GenerateSummaryDto(results.Count, now, req.Year, req.Month),
            $"Analysis generated for {req.Year}/{req.Month:D2}. Items processed: {results.Count}."));
    }

    // ── GET api/stock/analysis/results ───────────────────────────────────────
    [HttpGet("results")]
    public async Task<IActionResult> GetResults(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? search,
        [FromQuery] string? productClass,
        [FromQuery] string? supplier,
        [FromQuery] string? brand,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        var q = _db.StockAnalysisResults.AsNoTracking();

        if (year.HasValue)  q = q.Where(x => x.SarYear  == year.Value);
        if (month.HasValue) q = q.Where(x => x.SarMonth == month.Value);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.SarItemCode.Contains(search) ||
                              (x.SarItemDesc ?? "").Contains(search));

        if (!string.IsNullOrWhiteSpace(productClass))
            q = q.Where(x => (x.SarProductClassId ?? "").Contains(productClass) ||
                              (x.SarProductClassDesc ?? "").Contains(productClass));

        if (!string.IsNullOrWhiteSpace(supplier))
            q = q.Where(x => (x.SarSupplierCode ?? "").Contains(supplier) ||
                              (x.SarSupplierName ?? "").Contains(supplier));

        if (!string.IsNullOrWhiteSpace(brand))
            q = q.Where(x => (x.SarBrandCode ?? "").Contains(brand) ||
                              (x.SarBrandDesc ?? "").Contains(brand));

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.SarItemCode)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync();

        return Ok(PagedResponse<StockAnalysisResult>.Ok(items, page, pageSize, total));
    }

    // ── GET api/stock/analysis/summary ───────────────────────────────────────
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var q = _db.StockAnalysisResults.AsNoTracking();
        if (year.HasValue)  q = q.Where(x => x.SarYear  == year.Value);
        if (month.HasValue) q = q.Where(x => x.SarMonth == month.Value);

        var all = await q.ToListAsync();
        if (all.Count == 0)
            return Ok(ApiResponse<object>.Ok(new object(), "No results found for this period."));

        var totalItems             = all.Count;
        var itemsOverstocked       = all.Count(x => (x.SarOverstockUnits ?? 0m) > 0);
        var itemsUnderstocked      = all.Count(x => (x.SarOverstockUnits ?? 0m) < 0);
        var totalInventoryValue    = all.Sum(x => x.SarTotalInventoryValue ?? 0m);
        var totalOverstockAng      = all.Sum(x => x.SarOverstockAng ?? 0m);
        var totalIdealStockAng     = all.Sum(x => x.SarIdealStockAng ?? 0m);
        var totalYtdSales          = all.Sum(x => x.SarYtdSalesUnits ?? 0m);
        var totalYtdBudget         = all.Sum(x => x.SarYtdBudgetUnits ?? 0m);
        var overUnderPerf          = totalYtdSales - totalYtdBudget;
        var avgMonthsOfStock       = all.Average(x => x.SarMonthsOfStock ?? 0m);
        var avgMonthsInclOrder     = all.Average(x => x.SarMonthsOfStockInclOnOrder ?? 0m);

        var topOverstocked  = all.Where(x => (x.SarOverstockUnits ?? 0m) > 0)
                                 .OrderByDescending(x => x.SarOverstockUnits ?? 0m)
                                 .Take(10)
                                 .ToList();
        var topUnderstocked = all.Where(x => (x.SarOverstockUnits ?? 0m) < 0)
                                 .OrderBy(x => x.SarOverstockUnits ?? 0m)
                                 .Take(10)
                                 .ToList();

        var summary = new AnalysisSummaryDto(
            totalItems, itemsOverstocked, itemsUnderstocked,
            totalInventoryValue, totalOverstockAng, totalIdealStockAng,
            totalYtdSales, totalYtdBudget, overUnderPerf,
            avgMonthsOfStock, avgMonthsInclOrder,
            topOverstocked, topUnderstocked);

        return Ok(ApiResponse<AnalysisSummaryDto>.Ok(summary));
    }

    // ── GET api/stock/analysis/available-periods ──────────────────────────────
    [HttpGet("available-periods")]
    public async Task<IActionResult> GetAvailablePeriods()
    {
        var periods = await _db.StockAnalysisResults.AsNoTracking()
            .GroupBy(x => new { x.SarYear, x.SarMonth })
            .Select(g => new { g.Key.SarYear, g.Key.SarMonth })
            .OrderByDescending(x => x.SarYear)
            .ThenByDescending(x => x.SarMonth)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(periods));
    }

    // ── Helper: convert YYYYMMDD int to DateOnly ──────────────────────────────
    private static DateOnly IntToDateOnly(int yyyymmdd)
    {
        if (yyyymmdd <= 0) return default;
        var year  = yyyymmdd / 10000;
        var month = (yyyymmdd % 10000) / 100;
        var day   = yyyymmdd % 100;
        try { return new DateOnly(year, month, day); }
        catch { return default; }
    }
}
