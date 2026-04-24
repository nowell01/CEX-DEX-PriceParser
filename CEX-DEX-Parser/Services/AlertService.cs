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
    }
}
