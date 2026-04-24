using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class BybitClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<BybitClient> _logger;

        public BybitClient(IHttpClientFactory factory, ILogger<BybitClient> logger)
        {
            _http = factory.CreateClient("Bybit");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            // Bybit V5 uses concatenated symbols: BTC/USDT -> BTCUSDT
            var bybitSymbol = symbol.Replace("/", "");
            try
            {
                var response = await _http.GetAsync(
                    $"/v5/market/tickers?category=spot&symbol={bybitSymbol}");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                var list = root.GetProperty("result").GetProperty("list");
                if (list.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Bybit: empty ticker list for {Symbol}", symbol);
                    return null;
                }

                var ticker = list[0];

                return new ExchangePrice
                {
                    Exchange = "Bybit",
                    Symbol = symbol,
                    Price = decimal.Parse(ticker.GetProperty("lastPrice").GetString()!),
                    Volume24h = decimal.Parse(ticker.GetProperty("volume24h").GetString()!),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Bybit fetch failed for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }
    }
}
