using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class PriceMonitorService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PriceMonitorService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

        public PriceMonitorService(IServiceProvider services, ILogger<PriceMonitorService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Price monitor started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunCheckAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in price monitor cycle.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task RunCheckAsync()
        {
            // Resolve scoped services manually since BackgroundService is a singleton
            using var scope = _services.CreateScope();
            var exchangeService = scope.ServiceProvider.GetRequiredService<ExchangeService>();
            var alertService = scope.ServiceProvider.GetRequiredService<AlertService>();
            var storage = scope.ServiceProvider.GetRequiredService<JsonStorageService>();
            var detector = scope.ServiceProvider.GetRequiredService<ArbitrageDetector>();

            var watchlist = await storage.ReadAsync<TradingPair>("watchlist.json");
            if (watchlist.Count == 0)
                return;

            var configs = await alertService.GetConfigsAsync();
            if (configs.Count == 0)
                return;

            var comparisons = await exchangeService.GetAllComparisonsAsync(watchlist.Select(p => p.Symbol));
            var triggered = detector.Check(comparisons, configs);

            foreach (var alert in triggered)
            {
                _logger.LogInformation(
                    "Alert triggered: {Symbol} spread {Spread}% ({High} vs {Low})",
                    alert.Symbol, alert.SpreadPercent, alert.HighExchange, alert.LowExchange);

                await alertService.LogAndBroadcastAsync(alert);
            }
        }
    }
}
