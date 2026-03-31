using CEX_DEX_Parser.Hubs;
using CEX_DEX_Parser.Models;
using Microsoft.AspNetCore.SignalR;

namespace CEX_DEX_Parser.Services
{
    public class AlertService
    {
        private readonly JsonStorageService _storage;
        private readonly IHubContext<AlertHub> _hub;

        public AlertService(JsonStorageService storage, IHubContext<AlertHub> hub)
        {
            _storage = storage;
            _hub = hub;
        }

        public async Task LogAndBroadcastAsync(AlertLog alert)
        {
            await _storage.AppendAsync("alerts-log.json", alert);
            await _hub.Clients.All.SendAsync("AlertTriggered", alert);
        }

        public Task<List<AlertLog>> GetHistoryAsync() =>
            _storage.ReadAsync<AlertLog>("alerts-log.json");

        public Task<List<AlertConfig>> GetConfigsAsync() =>
            _storage.ReadAsync<AlertConfig>("alerts-config.json");

        public async Task SaveConfigAsync(AlertConfig config)
        {
            var configs = await GetConfigsAsync();
            var existing = configs.FindIndex(c =>
                string.Equals(c.Symbol, config.Symbol, StringComparison.OrdinalIgnoreCase));

            if (existing >= 0)
                configs[existing] = config;
            else
                configs.Add(config);

            await _storage.WriteAsync("alerts-config.json", configs);
        }

        public async Task DeleteConfigAsync(string symbol)
        {
            var configs = await GetConfigsAsync();
            configs.RemoveAll(c => string.Equals(c.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
            await _storage.WriteAsync("alerts-config.json", configs);
        }
    }
}
