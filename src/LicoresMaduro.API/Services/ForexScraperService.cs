using LicoresMaduro.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace LicoresMaduro.API.Services;

public interface IForexScraperService
{
    Task<int> ScrapeAndSaveRatesAsync(CancellationToken ct = default);
}

public class ForexScraperService : IForexScraperService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ForexScraperService> _logger;

    private static readonly string ProfileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LicoresMaduro", "ForexEdgeProfile");

    private const string ForexUrl = "https://webportal.centralbank.cw/forex/frm06_by_day.aspx";

    // Matches data rows: <tr ... bgcolor="#ebedf3" ...> ... </tr>
    private static readonly Regex RowRx = new(
        @"<tr[^>]*bgcolor\s*=\s*""#ebedf3""[^>]*>(.*?)</tr>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches each <td>...</td> within a row
    private static readonly Regex CellRx = new(
        @"<td[^>]*>(.*?)</td>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Strips any remaining HTML tags
    private static readonly Regex TagRx = new(
        @"<[^>]+>",
        RegexOptions.Compiled);

    public ForexScraperService(ApplicationDbContext db, ILogger<ForexScraperService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> ScrapeAndSaveRatesAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(ProfileDir);

        string edgePath = FindEdgePath();
        if (string.IsNullOrEmpty(edgePath))
            throw new InvalidOperationException("Microsoft Edge not found. Please install Edge.");

        int debugPort = GetFreePort();
        _logger.LogInformation("Launching real Edge on debug port {Port}...", debugPort);

        var psi = new ProcessStartInfo(edgePath,
            $"--remote-debugging-port={debugPort} " +
            $"--user-data-dir=\"{ProfileDir}\" " +
            "--no-first-run --no-default-browser-check " +
            "--disable-blink-features=AutomationControlled " +
            "about:blank")
        {
            UseShellExecute = true
        };

        using var edgeProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to launch Edge.");

        try
        {
            await WaitForCdpAsync(debugPort, ct);

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.ConnectOverCDPAsync(
                $"http://localhost:{debugPort}",
                new BrowserTypeConnectOverCDPOptions { Timeout = 15_000 });

            try
            {
                var bContext = browser.Contexts.FirstOrDefault()
                    ?? throw new InvalidOperationException("No browser context.");
                var page = bContext.Pages.FirstOrDefault() ?? await bContext.NewPageAsync();

                _logger.LogInformation("Navigating to {Url}...", ForexUrl);
                await page.GotoAsync(ForexUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout   = 60_000
                });

                _logger.LogInformation("Waiting for forex table (max 3 min)...");
                bool tableFound = false;
                try
                {
                    await page.WaitForFunctionAsync(
                        "() => document.body.innerText.toUpperCase().includes('US DOLLAR')",
                        options: new PageWaitForFunctionOptions
                        {
                            Timeout         = 180_000,
                            PollingInterval = 2_000
                        });
                    tableFound = true;
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Timed out waiting for forex table.");
                }

                if (!tableFound)
                    return 0;

                // Get the raw HTML and parse in C# — avoids EvaluateAsync deserialization issues
                var html = await page.ContentAsync();
                var rows = ParseForexRows(html);
                _logger.LogInformation("Rows parsed from HTML: {Total}.", rows.Count);

                int saved = 0;
                if (rows.Count > 0)
                {
                    var today = DateTime.Today;
                    foreach (var row in rows)
                    {
                        string code = InferCode(row.Description);
                        if (code == "UNKNOWN")
                        {
                            _logger.LogDebug("Skipping unknown currency: {Desc}", row.Description);
                            continue;
                        }

                        bool exists = await _db.CcForexHistories
                            .AnyAsync(x => x.FxDate == today && x.FxCurrency == code, ct);
                        if (exists) continue;

                        int divisor = GetDivisor(row.Description);
                        _db.CcForexHistories.Add(new CcForexHistory
                        {
                            FxDate        = today,
                            FxCurrency    = code,
                            FxDescription = row.Description,
                            FxBuyingCash  = ParseRate(row.BuyingCash),
                            FxBuyingCheck = ParseRate(row.BuyingCheck),
                            FxSellingRate = DivideRate(ParseRate(row.SellingRate), divisor),
                            FxCreatedAt   = DateTime.UtcNow
                        });
                        saved++;
                    }
                    if (saved > 0) await _db.SaveChangesAsync(ct);
                }

                _logger.LogInformation("Forex scrape complete — {Saved} new rate(s) saved.", saved);
                return saved;
            }
            finally
            {
                try { await browser.CloseAsync(); } catch { }
            }
        }
        finally
        {
            try { if (!edgeProcess.HasExited) edgeProcess.Kill(entireProcessTree: true); } catch { }
        }
    }

    // Parse data rows from HTML using regex — no JS evaluation needed
    private List<ForexRow> ParseForexRows(string html)
    {
        var result = new List<ForexRow>();

        foreach (Match rowMatch in RowRx.Matches(html))
        {
            var cells = CellRx.Matches(rowMatch.Groups[1].Value)
                .Select(m => CleanHtml(m.Groups[1].Value))
                .ToList();

            if (cells.Count < 4 || string.IsNullOrWhiteSpace(cells[0]))
                continue;

            result.Add(new ForexRow
            {
                Description = cells[0],
                BuyingCash  = cells[1],   // td[1] = Bank-notes
                BuyingCheck = cells[2],   // td[2] = Cheques & Transfers
                SellingRate = cells[3]    // td[3] = Selling Rate
            });
        }

        return result;
    }

    private string CleanHtml(string html) =>
        System.Web.HttpUtility.HtmlDecode(
            TagRx.Replace(html, " "))
        .Replace(" ", " ")   // non-breaking space
        .Replace("\n", " ")
        .Replace("\r", " ")
        .Trim();

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string FindEdgePath()
    {
        string[] candidates =
        [
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
        ];
        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForCdpAsync(int port, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        for (int i = 0; i < 25; i++)
        {
            try
            {
                var r = await http.GetAsync($"http://localhost:{port}/json/version", ct);
                if (r.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(400, ct);
        }
        throw new TimeoutException("Edge CDP endpoint did not become available.");
    }

    private static string InferCode(string description)
    {
        string d = description.ToUpperInvariant();
        if (d.Contains("US DOLLAR"))                                       return "USD";
        if (d.Contains("JAPANESE YEN"))                                    return "JPY";
        if (d.Contains("EURO"))                                            return "EUR";
        if (d.Contains("BRITISH POUND") || d.Contains("POUND STERLING"))  return "GBP";
        if (d.Contains("CANADIAN DOLLAR"))                                 return "CAD";
        if (d.Contains("SWISS FRANC"))                                     return "CHF";
        if (d.Contains("AUSTRALIAN DOLLAR"))                               return "AUD";
        if (d.Contains("SWEDISH") || d.Contains("KRONA"))                 return "SEK";
        if (d.Contains("NORWEGIAN") || d.Contains("KRONE"))               return "NOK";
        if (d.Contains("DANISH"))                                          return "DKK";
        if (d.Contains("CARIBBEAN GUILDER") || d.Contains("ARUBAN") ||
            d.Contains("NETH ANT"))                                        return "AWG";
        if (d.Contains("TRINIDAD"))                                        return "TTD";
        return "UNKNOWN";
    }

    private static decimal? ParseRate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        string clean = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        return decimal.TryParse(clean.Replace(",", "."), NumberStyles.Any,
            CultureInfo.InvariantCulture, out decimal val) ? val : null;
    }

    private static int GetDivisor(string description)
    {
        string d = description.ToUpperInvariant();
        if (d.Contains("PER 10000")) return 10000;
        if (d.Contains("PER 100"))   return 100;
        return 1;
    }

    private static decimal? DivideRate(decimal? rate, int divisor) =>
        rate.HasValue && divisor > 1 ? rate.Value / divisor : rate;

    private sealed class ForexRow
    {
        public string Description { get; set; } = string.Empty;
        public string BuyingCash  { get; set; } = string.Empty;
        public string BuyingCheck { get; set; } = string.Empty;
        public string SellingRate { get; set; } = string.Empty;
    }
}
