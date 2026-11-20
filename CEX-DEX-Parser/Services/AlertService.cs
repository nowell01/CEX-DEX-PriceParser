using CEX_DEX_Parser.Hubs;
using CEX_DEX_Parser.Models;
using CEX_DEX_Parser.Services.Storage;
using Microsoft.AspNetCore.SignalR;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

namespace CEX_DEX_Parser.Services
{
    public class AlertService
    {
        private readonly IAlertLogStore _alerts;
        private readonly IHubContext<AlertHub> _hub;
        private readonly ITelegramNotifier _telegram;
        private readonly ILogger<AlertService> _logger;

        public AlertService(IAlertLogStore alerts, IHubContext<AlertHub> hub, ITelegramNotifier telegram, ILogger<AlertService> logger)
        {
            _alerts = alerts;
            _hub = hub;
            _telegram = telegram;
            _logger = logger;
        }

        public async Task LogAndBroadcastAsync(AlertLog alert)
        {
            await _alerts.AddAsync(alert);
            await _hub.Clients.All.SendAsync("AlertTriggered", alert);

            // key = symbol -> enables the per-symbol 4-minute cooldown in TelegramNotifier
            await _telegram.SendAsync(TelegramAlertFormatter.Format(alert), key: alert.Symbol);

            _logger.LogInformation(
                "Alert fired: {Symbol} spread {SpreadPercent:F2}% - buy on {LowExchange}, sell on {HighExchange}",
                alert.Symbol, alert.SpreadPercent, alert.LowExchange, alert.HighExchange);
        }

        public Task<List<AlertLog>> GetHistoryAsync(int take = 200) =>
            _alerts.GetRecentAsync(take);

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