using System.Text;
using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class HyperliquidClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<HyperliquidClient> _logger;

        public HyperliquidClient(IHttpClientFactory factory, ILogger<HyperliquidClient> logger)
        {
            _http = factory.CreateClient("Hyperliquid");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            // Hyperliquid uses base asset only for perpetuals: BTC/USDT -> BTC
            var baseAsset = symbol.Split('/')[0].ToUpper();

            try
            {
                // Hyperliquid Info API: POST with body { "type": "allMids" }
                var requestBody = new StringContent(
                    "{\"type\":\"allMids\"}",
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.PostAsync("/info", requestBody);
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                if (!root.TryGetProperty(baseAsset, out var priceElement))
                {
                    _logger.LogWarning("Hyperliquid does not list {Asset}", baseAsset);
                    return null;
                }

                return new ExchangePrice
                {
                    Exchange = "Hyperliquid",
                    Symbol = symbol,
                    Price = decimal.Parse(priceElement.GetString()!),
                    Volume24h = 0, // allMids endpoint does not return volume
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Hyperliquid fetch failed for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }
    }
}
