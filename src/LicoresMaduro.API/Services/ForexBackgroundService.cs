namespace LicoresMaduro.API.Services;

public class ForexBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ForexBackgroundService> _logger;

    public ForexBackgroundService(IServiceProvider serviceProvider, ILogger<ForexBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private static readonly TimeOnly RunAt = new(6, 0, 0); // 6:00 AM

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Forex Background Service starting — daily run scheduled at {Time}.", RunAt);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNext(RunAt);
            _logger.LogInformation("Forex task sleeping {Hours:F1} h until next run at {Time}.", delay.TotalHours, RunAt);
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("Executing daily Forex scraping task...");
                using var scope = _serviceProvider.CreateScope();
                var scraper = scope.ServiceProvider.GetRequiredService<IForexScraperService>();
                int count = await scraper.ScrapeAndSaveRatesAsync(stoppingToken);
                _logger.LogInformation("Forex task completed. {Count} rates processed.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Forex scraping task.");
            }
        }
    }

    private static TimeSpan TimeUntilNext(TimeOnly target)
    {
        var now  = DateTime.Now;
        var next = DateTime.Today.Add(target.ToTimeSpan());
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}
