using CEX_DEX_Parser.Hubs;
using CEX_DEX_Parser.Models;
using Microsoft.AspNetCore.SignalR;

namespace CEX_DEX_Parser.Services
{
    public class AlertService
    {
        private readonly JsonStorageService _storage;
        private readonly IHubContext<AlertHub> _hub;
        private readonly ITelegramNotifier _telegram;
        private readonly ILogger<AlertService> _logger;

        public AlertService(JsonStorageService storage, IHubContext<AlertHub> hub, ITelegramNotifier telegram, ILogger<AlertService> logger)
        {
            _storage = storage;
            _hub = hub;
            _telegram = telegram;
            _logger = logger;
        }

        public async Task LogAndBroadcastAsync(AlertLog alert)
        {
            await _storage.AppendAsync("alerts-log.json", alert);
            await _hub.Clients.All.SendAsync("AlertTriggered", alert);
            await _telegram.SendAsync(TelegramAlertFormatter.Format(alert));

            _logger.LogInformation("Alert fired: {Symbol} spread {SpreadPercent:F2}% — buy on {LowExchange}, sell on {HighExchange}",
            alert.Symbol, alert.SpreadPercent, alert.LowExchange, alert.HighExchange);
        }

        public Task<List<AlertLog>> GetHistoryAsync() =>
            _storage.ReadAsync<AlertLog>("alerts-log.json");
        public static class TelegramAlertFormatter
        {
            public static string Format(AlertLog alert)
            {
                return $"""
             <b>Arbitrage Alert {alert.Symbol}</b>

             <b>Buy on:</b>  {alert.LowExchange}  @ ${alert.LowPrice:N2}
             <b>Sell on:</b> {alert.HighExchange} @ ${alert.HighPrice:N2}

             <b>Spread:</b> {alert.SpreadPercent:F2}%
             {alert.TriggeredAt:yyyy-MM-dd HH:mm:ss} UTC
            """;
            }
        }
    }
}
