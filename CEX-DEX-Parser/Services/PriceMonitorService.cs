using CEX_DEX_Parser.Services.Storage;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

namespace CEX_DEX_Parser.Services
{
    public class PriceMonitorService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PriceMonitorService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        // Same default pairs as PricesController — single source of truth
        private static readonly string[] DefaultPairs =
        {
            "BTC/USDT", "ETH/USDT", "SOL/USDT", "XRP/USDT", "SUI/USDT",
            "DOGE/USDT", "ADA/USDT", "LTC/USDT", "AVAX/USDT", "GRAM/USDT",
            "AAVE/USDT", "APEX/USDT", "ARB/USDT", "BNB/USDT", "STRK/USDT",
            "TRX/USDT", "PEPE/USDT", "LINK/USDT", "TRUMP/USDT", "SHIB/USDT",
            "XLM/USDT"
        };

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
            var priceHistory = scope.ServiceProvider.GetRequiredService<IPriceHistoryStore>();
            var detector = scope.ServiceProvider.GetRequiredService<ArbitrageDetector>();

            var comparisons = await exchangeService.GetAllComparisonsAsync(DefaultPairs);

            // Save the whole scan in one database write (was one full-file rewrite per symbol)
            try
            {
                await priceHistory.AddManyAsync(comparisons);
            }
            catch (Exception ex)
            {
                // A database hiccup must not stop arbitrage detection/alerts for this cycle
                _logger.LogError(ex, "Failed to save price history for this cycle.");
            }

            // Check for arbitrage: alerts trigger when spread > 3%
            var triggered = detector.Check(comparisons);

            foreach (var alert in triggered)
            {
                _logger.LogInformation(
                    "ARBITRAGE ALERT: {Symbol} spread {Spread}% ({High} @ {HighPrice} vs {Low} @ {LowPrice})",
                    alert.Symbol, alert.SpreadPercent,
                    alert.HighExchange, alert.HighPrice,
                    alert.LowExchange, alert.LowPrice);

                await alertService.LogAndBroadcastAsync(alert);
            }
        }
    }
}