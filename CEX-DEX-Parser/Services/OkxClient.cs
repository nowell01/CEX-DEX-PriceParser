using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class OkxClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<OkxClient> _logger;

        public OkxClient(IHttpClientFactory factory, ILogger<OkxClient> logger)
        {
            _http = factory.CreateClient("OKX");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            // OKX uses dash-separated instrument IDs: BTC/USDT -> BTC-USDT
            var instId = symbol.Replace("/", "-");
            try
            {
                var response = await _http.GetAsync(
                    $"/api/v5/market/ticker?instId={instId}");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                var data = root.GetProperty("data");
                if (data.GetArrayLength() == 0)
                {
                    _logger.LogWarning("OKX: empty data array for {Symbol}", symbol);
                    return null;
                }

                var ticker = data[0];

                // volCcy24h = 24h trading volume in quote currency (e.g. USDT)
                decimal volume = 0;
                if (ticker.TryGetProperty("volCcy24h", out var volCcy) &&
                    volCcy.GetString() is string volStr)
                {
                    decimal.TryParse(volStr, out volume);
                }

                return new ExchangePrice
                {
                    Exchange = "OKX",
                    Symbol = symbol,
                    Price = decimal.Parse(ticker.GetProperty("last").GetString()!),
                    Volume24h = volume,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OKX fetch failed for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }
    }
}
