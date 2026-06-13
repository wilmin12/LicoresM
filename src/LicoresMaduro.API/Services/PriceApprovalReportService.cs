using ClosedXML.Excel;
using LicoresMaduro.API.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LicoresMaduro.API.Services;

public interface IPriceApprovalReportService
{
    /// <summary>Generates the VIP price-change Excel and the two approval PDFs for a confirmed PO.</summary>
    Task GenerateAsync(int calcId, string poNo, CancellationToken ct);
}

public sealed class PriceApprovalReportService : IPriceApprovalReportService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PriceApprovalReportService> _logger;

    public PriceApprovalReportService(ApplicationDbContext db, ILogger<PriceApprovalReportService> logger)
    { _db = db; _logger = logger; }

    public async Task GenerateAsync(int calcId, string poNo, CancellationToken ct)
    {
        try
        {
            var rows = await _db.CcPriceConfirmations.AsNoTracking()
                .Where(x => x.CcpcCalcNumber == calcId && x.CcpcPoNo == poNo)
                .OrderBy(x => x.CcpcItemNo)
                .ToListAsync(ct);
            if (rows.Count == 0) { _logger.LogWarning("PriceReport: no rows for Calc #{Id} PO {Po}", calcId, poNo); return; }

            var po = await _db.CcCalcPoHeads.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CcphCalcNumber == calcId && p.CcphLmPoNo == poNo, ct);
            var calc = await _db.CcCalcHeaders.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CcCalcNumber == calcId, ct);
            var unitsByItem = await _db.CcCalcPoDetails.AsNoTracking()
                .Where(d => d.CcpdCalcNumber == calcId && d.CcpdLmPoNo == poNo)
                .ToDictionaryAsync(d => d.CcpdItemNo, d => d.CcpdUnitCase ?? 12, ct);
            var sysCfg = await _db.SystemTable.AsNoTracking().FirstOrDefaultAsync(ct);

            decimal norsa    = sysCfg?.CompStoreNorsaPerc    ?? 0m;
            decimal retail   = sysCfg?.CompStoreRetailPerc   ?? 0m;
            decimal alliance = sysCfg?.CompStoreAlliancePerc ?? 0m;

            var ctx = new ReportContext(calcId, poNo, po, calc, rows, unitsByItem, norsa, retail, alliance);

            GenerateVipPriceExcel(ctx, sysCfg?.CompPathCostChanges);
            GenerateApprovedPriceAnalysisPdf(ctx, sysCfg?.CompPathPriceChanges);
            GenerateFinalPriceChangeReportPdf(ctx, sysCfg?.CompPathPriceChanges);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PriceApprovalReportService failed for Calc #{Id} PO {Po}", calcId, poNo);
        }
    }

    private sealed record ReportContext(
        int CalcId, string PoNo, CcCalcPoHead? Po, CcCalcHeader? Calc,
        List<CcPriceConfirmation> Rows, Dictionary<string, int> UnitsByItem,
        decimal Norsa, decimal Retail, decimal Alliance);

    private static int Units(ReportContext c, string item) =>
        c.UnitsByItem.TryGetValue(item, out var u) && u > 0 ? u : 12;

    private static decimal Btl(decimal? caseVal, int units) =>
        units > 0 ? Math.Round((caseVal ?? 0m) / units, 2) : (caseVal ?? 0m);

    // ── 1. VIP price-change Excel (layout 1043.xlsx) — only changed items ──────
    private void GenerateVipPriceExcel(ReportContext c, string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) { _logger.LogWarning("PriceReport: Cost Changes Path not configured, skipping VIP price Excel."); return; }
        Directory.CreateDirectory(folder);

        var changed = c.Rows.Where(r => r.CcpcPriceChangeFlag == true).ToList();
        if (changed.Count == 0) { _logger.LogInformation("PriceReport: no items flagged for price change in Calc #{Id} PO {Po}; VIP price Excel skipped.", c.CalcId, c.PoNo); return; }

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("VIP Price");

        string[] headers =
        [
            "CCPD_Price_Change_Sel", "CCPD_Calc_Number", "CCPD_LMPoNo",
            "CCPH_Created_By", "CCPH_Confirmed_By", "CCPH_Approved_By", "CCPH_Price_Change_Doc_Seq",
            "ITEM_NUMBER", "ITEM_DESCRIPTION", "CCPD_Selling_Units", "Old_Selling_Price_11010_Btl",
            "PR_01_WHOLESALE_CASE", "PR_01_WHOLESALE_BTL", "NORSA_PERC", "RETAIL_PERC", "ALLIANCE_PERC",
            "PR_03_NORSA_CASE", "PR_03_NORSA_BTL", "PR_04_RETAIL_CASE", "PR_04_RETAIL_BTL",
            "PR_05_ALLIANCE_CASE", "PR_05_ALLIANCE_BTL", "PR_06_BONDED_CASE", "PR_06_BONDED_BTL",
            "PRICE_DIFFERENCE_PR_06", "PR_07_SPECIAL_BONDED_CASE", "PR_07_SPECIAL_BONDED_BTL",
            "PR_08_GWC_MANG_ESP_BONDED_CASE", "PR_08_GWC_MANG_ESP_BONDED_BTL",
            "PR_09_YU_HUA_BONDED_CASE", "PR_09_YU_HUA_BONDED_BTL",
            "PR_10_BBB_DUTY_PAID_CASE", "PR_10_BBB_DUTY_PAID_BTL",
            "PR_11_BBB_BONDED_CASE", "PR_11_BBB_BONDED_BTL", "Old_Selling_Price_11060_Btl"
        ];
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var r in changed)
        {
            int u = Units(c, r.CcpcItemNo);
            double D(decimal? v) => (double)Math.Round(v ?? 0m, 2);

            ws.Cell(row, 1).Value  = true;
            ws.Cell(row, 2).Value  = c.CalcId;
            ws.Cell(row, 3).Value  = c.PoNo;
            ws.Cell(row, 4).Value  = c.Po?.CcphCreatedBy ?? c.Calc?.CcCreatedBy ?? string.Empty;
            ws.Cell(row, 5).Value  = c.Po?.CcphConfirmedBy ?? string.Empty;
            ws.Cell(row, 6).Value  = c.Po?.CcphApprovedBy ?? string.Empty;
            ws.Cell(row, 7).Value  = string.Empty; // Price change doc sequence (reserved)
            ws.Cell(row, 8).Value  = r.CcpcItemNo;
            ws.Cell(row, 9).Value  = r.CcpcDescription ?? string.Empty;
            ws.Cell(row, 10).Value = u;
            ws.Cell(row, 11).Value = (double)Btl(r.CcpcOldPricePr01, u);
            ws.Cell(row, 12).Value = D(r.CcpcNewPricePr01);
            ws.Cell(row, 13).Value = (double)Btl(r.CcpcNewPricePr01, u);
            ws.Cell(row, 14).Value = (double)c.Norsa;
            ws.Cell(row, 15).Value = (double)c.Retail;
            ws.Cell(row, 16).Value = (double)c.Alliance;
            ws.Cell(row, 17).Value = D(r.CcpcNewPricePr03);
            ws.Cell(row, 18).Value = (double)Btl(r.CcpcNewPricePr03, u);
            ws.Cell(row, 19).Value = D(r.CcpcNewPricePr04);
            ws.Cell(row, 20).Value = (double)Btl(r.CcpcNewPricePr04, u);
            ws.Cell(row, 21).Value = D(r.CcpcNewPricePr05);
            ws.Cell(row, 22).Value = (double)Btl(r.CcpcNewPricePr05, u);
            ws.Cell(row, 23).Value = D(r.CcpcNewPricePr06);
            ws.Cell(row, 24).Value = (double)Btl(r.CcpcNewPricePr06, u);
            ws.Cell(row, 25).Value = D((r.CcpcNewPricePr06 ?? 0m) - (r.CcpcOldPricePr06 ?? 0m));
            ws.Cell(row, 26).Value = D(r.CcpcNewPricePr07);
            ws.Cell(row, 27).Value = (double)Btl(r.CcpcNewPricePr07, u);
            ws.Cell(row, 28).Value = D(r.CcpcNewPricePr08);
            ws.Cell(row, 29).Value = (double)Btl(r.CcpcNewPricePr08, u);
            ws.Cell(row, 30).Value = D(r.CcpcNewPricePr09);
            ws.Cell(row, 31).Value = (double)Btl(r.CcpcNewPricePr09, u);
            ws.Cell(row, 32).Value = D(r.CcpcNewPricePr10);
            ws.Cell(row, 33).Value = (double)Btl(r.CcpcNewPricePr10, u);
            ws.Cell(row, 34).Value = D(r.CcpcNewPricePr11);
            ws.Cell(row, 35).Value = (double)Btl(r.CcpcNewPricePr11, u);
            ws.Cell(row, 36).Value = (double)Btl(r.CcpcOldPricePr06, u);
            row++;
        }

        ws.Columns().AdjustToContents();
        var filePath = Path.Combine(folder, $"{c.PoNo.Replace("/", "_")}_PRICE.xlsx");
        wb.SaveAs(filePath);
        _logger.LogInformation("PriceReport: VIP price Excel saved to {Path}", filePath);
    }

    // ── 2. Approved Price Analysis by Sales Manager (PDF) ─────────────────────
    private void GenerateApprovedPriceAnalysisPdf(ReportContext c, string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) { _logger.LogWarning("PriceReport: Price Changes Path not configured, skipping Approved Price Analysis PDF."); return; }
        Directory.CreateDirectory(folder);
        QuestPDF.Settings.License = LicenseType.Community;

        static string N(decimal? v, int d = 2) => v.HasValue ? v.Value.ToString($"N{d}") : "–";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(6.5f).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("APPROVED PRICE ANALYSIS BY SALES MANAGER").FontSize(11).Bold();
                        r.ConstantItem(180).AlignRight().Text($"Order: {c.PoNo}   #{c.CalcId}").FontSize(9).Bold();
                    });
                    col.Item().PaddingBottom(2).Text($"Warehouse: {c.Calc?.CcWarehouse}    Forwarder: {c.Calc?.CcForwarderName}").FontSize(7);
                    col.Item().LineHorizontal(1);
                });

                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(38);  // item
                        cd.RelativeColumn(2.4f); // desc
                        cd.ConstantColumn(30);  // WH
                        foreach (var _ in Enumerable.Range(0, 11)) cd.RelativeColumn();
                        cd.ConstantColumn(34);  // price change
                    });

                    void HeadCell(string s) => t.Cell().Background(Color.FromHex("#eee")).Padding(2).Text(s).Bold().FontSize(6);
                    HeadCell("Item"); HeadCell("Description"); HeadCell("WH");
                    HeadCell("Allowed%"); HeadCell("Actual%"); HeadCell("New Cost"); HeadCell("Old Cost");
                    HeadCell("Chg Cost"); HeadCell("New Price"); HeadCell("Old Price"); HeadCell("Chg Price");
                    HeadCell("Chg Mrg"); HeadCell("New Price(Case)"); HeadCell("Changed");

                    foreach (var r in c.Rows)
                    {
                        var changed = r.CcpcPriceChangeFlag == true;
                        // 11010 sub-row (PR01)
                        AddAnalysisRow(t, r.CcpcItemNo, r.CcpcDescription, "11010",
                            r.CcpcAllowedMargin, r.CcpcNewMarginPr01, r.CcpcNewCost11010, r.CcpcOldCost11010,
                            r.CcpcNewPricePr01, r.CcpcOldPricePr01, changed, N);
                        // 11060 sub-row (PR06)
                        AddAnalysisRow(t, "", "", "11060",
                            r.CcpcAllowedMargin11060, r.CcpcNewMarginPr06, r.CcpcNewCost11060, r.CcpcOldCost11060,
                            r.CcpcNewPricePr06, r.CcpcOldPricePr06, false, N);
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Reason: " + (c.Po?.CcphReasonCode ?? "–") + "   Date: " +
                        (c.Po?.CcphPriceChangeDate?.ToString("yyyy-MM-dd") ?? "–") + "   ").FontSize(7);
                });
            });
        });

        var filePath = Path.Combine(folder, $"{c.PoNo.Replace("/", "_")}_ApprovedPriceAnalysis.pdf");
        doc.GeneratePdf(filePath);
        _logger.LogInformation("PriceReport: Approved Price Analysis PDF saved to {Path}", filePath);
    }

    private static void AddAnalysisRow(TableDescriptor t, string item, string? desc, string wh,
        decimal? allowed, decimal? actual, decimal? newCost, decimal? oldCost,
        decimal? newPrice, decimal? oldPrice, bool changed, Func<decimal?, int, string> N)
    {
        var chgCost  = (newCost ?? 0m) - (oldCost ?? 0m);
        var chgPrice = (newPrice ?? 0m) - (oldPrice ?? 0m);
        var chgMrg   = (actual ?? 0m); // margin shift shown as actual margin

        void C(string s, bool right = true) => t.Cell().Padding(1.5f)
            .AlignRight().Text(s).FontSize(6);

        t.Cell().Padding(1.5f).Text(item).FontSize(6);
        t.Cell().Padding(1.5f).Text(desc ?? "").FontSize(6);
        t.Cell().Padding(1.5f).Text(wh).FontSize(6);
        C(N(allowed, 2));
        C(N(actual, 2));
        C(N(newCost, 2));
        C(N(oldCost, 2));
        C(N(chgCost, 2));
        C(N(newPrice, 2));
        C(N(oldPrice, 2));
        C(N(chgPrice, 2));
        C(N(chgMrg, 2));
        C(N(newPrice, 2));
        t.Cell().Padding(1.5f).AlignCenter().Text(changed ? "✔" : "").FontSize(7);
    }

    // ── 3. Final Price Change Report (PDF) — only changed products ────────────
    private void GenerateFinalPriceChangeReportPdf(ReportContext c, string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) { _logger.LogWarning("PriceReport: Price Changes Path not configured, skipping Final Price Change Report PDF."); return; }
        Directory.CreateDirectory(folder);
        QuestPDF.Settings.License = LicenseType.Community;

        var changed = c.Rows.Where(r => r.CcpcPriceChangeFlag == true).ToList();
        if (changed.Count == 0) { _logger.LogInformation("PriceReport: no changed items, Final Price Change Report skipped."); return; }

        static string N(decimal? v) => v.HasValue ? v.Value.ToString("N2") : "–";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(7f).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("FINAL PRICE CHANGE REPORT").FontSize(12).Bold();
                        r.ConstantItem(160).AlignRight().Text($"#{c.CalcId}   {c.Po?.CcphPriceChangeDate?.ToString("dd-MMM-yy") ?? ""}").FontSize(9);
                    });
                    col.Item().PaddingVertical(2).Row(r =>
                    {
                        r.RelativeItem().Text($"Order: {c.PoNo}").FontSize(8);
                        r.RelativeItem().Text($"Created By: {c.Po?.CcphCreatedBy ?? c.Calc?.CcCreatedBy ?? "–"}").FontSize(8);
                        r.RelativeItem().Text($"Confirmed By: {c.Po?.CcphConfirmedBy ?? "–"}").FontSize(8);
                        r.RelativeItem().Text($"Approved By: {c.Po?.CcphApprovedBy ?? "–"}").FontSize(8);
                    });
                    col.Item().LineHorizontal(1);
                });

                page.Content().PaddingTop(4).Table(t =>
                {
                    string[] tiers = ["PR01 WS", "PR03 NORSA", "PR04 RETAIL", "PR05 ALLIANCE",
                        "PR06 BONDED", "PR07 SP.BOND", "PR08 GWC", "PR09 YUHUA", "PR10 BBB DP", "PR11 BBB BOND"];

                    t.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(38); // item
                        cd.RelativeColumn(2);  // desc
                        cd.ConstantColumn(28); // units
                        cd.ConstantColumn(30); // unit type
                        foreach (var _ in tiers) cd.RelativeColumn();
                    });

                    void HeadCell(string s) => t.Cell().Background(Color.FromHex("#eee")).Padding(2).Text(s).Bold().FontSize(6);
                    HeadCell("Item"); HeadCell("Description"); HeadCell("Units"); HeadCell("");
                    foreach (var tier in tiers) HeadCell(tier);

                    decimal?[] CasePrices(CcPriceConfirmation r) =>
                        [r.CcpcNewPricePr01, r.CcpcNewPricePr03, r.CcpcNewPricePr04, r.CcpcNewPricePr05,
                         r.CcpcNewPricePr06, r.CcpcNewPricePr07, r.CcpcNewPricePr08, r.CcpcNewPricePr09,
                         r.CcpcNewPricePr10, r.CcpcNewPricePr11];

                    foreach (var r in changed)
                    {
                        int u = Units(c, r.CcpcItemNo);
                        var cases = CasePrices(r);

                        // Case row
                        t.Cell().Padding(1.5f).Text(r.CcpcItemNo).FontSize(6);
                        t.Cell().Padding(1.5f).Text(r.CcpcDescription ?? "").FontSize(6);
                        t.Cell().Padding(1.5f).AlignRight().Text(u.ToString()).FontSize(6);
                        t.Cell().Padding(1.5f).Text("CS/UNIT").FontSize(6);
                        foreach (var p in cases) t.Cell().Padding(1.5f).AlignRight().Text(N(p)).FontSize(6);

                        // Bottle row
                        t.Cell().Padding(1.5f).Text("").FontSize(6);
                        t.Cell().Padding(1.5f).Text("").FontSize(6);
                        t.Cell().Padding(1.5f).Text("").FontSize(6);
                        t.Cell().Padding(1.5f).Text("P/UNIT").FontSize(6);
                        foreach (var p in cases) t.Cell().Padding(1.5f).AlignRight().Text(N(Btl(p, u))).FontSize(6);
                    }
                });
            });
        });

        var filePath = Path.Combine(folder, $"{c.PoNo.Replace("/", "_")}_FinalPriceChange.pdf");
        doc.GeneratePdf(filePath);
        _logger.LogInformation("PriceReport: Final Price Change Report PDF saved to {Path}", filePath);
    }
}
