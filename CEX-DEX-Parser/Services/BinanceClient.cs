using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class BinanceClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<BinanceClient> _logger;

        public BinanceClient(IHttpClientFactory factory, ILogger<BinanceClient> logger)
        {
            _http = factory.CreateClient("Binance");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            // Binance uses concatenated symbols: BTC/USDT -> BTCUSDT
            var binanceSymbol = symbol.Replace("/", "");
            try
            {
                var response = await _http.GetAsync($"/api/v3/ticker/24hr?symbol={binanceSymbol}");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                return new ExchangePrice
                {
                    Exchange = "Binance",
                    Symbol = symbol,
                    Price = decimal.Parse(root.GetProperty("lastPrice").GetString()!),
                    Volume24h = decimal.Parse(root.GetProperty("quoteVolume").GetString()!),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Binance fetch failed for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }
    }
}
