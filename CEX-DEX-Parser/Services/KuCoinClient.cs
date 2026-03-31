using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class KuCoinClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<KuCoinClient> _logger;

        public KuCoinClient(IHttpClientFactory factory, ILogger<KuCoinClient> logger)
        {
            _http = factory.CreateClient("KuCoin");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            // KuCoin uses dash separator: BTC/USDT -> BTC-USDT
            var kuCoinSymbol = symbol.Replace("/", "-");
            try
            {
                var response = await _http.GetAsync($"/api/v1/market/orderbook/level1?symbol={kuCoinSymbol}");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var data = doc.RootElement.GetProperty("data");

                if (data.ValueKind == JsonValueKind.Null)
                {
                    _logger.LogWarning("KuCoin returned null data for {Symbol}", symbol);
                    return null;
                }

                return new ExchangePrice
                {
                    Exchange = "KuCoin",
                    Symbol = symbol,
                    Price = decimal.Parse(data.GetProperty("price").GetString()!),
                    Volume24h = 0,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("KuCoin fetch failed for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }
    }
}
