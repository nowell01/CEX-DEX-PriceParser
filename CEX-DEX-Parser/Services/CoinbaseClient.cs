using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class CoinbaseClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<CoinbaseClient> _logger;

        public CoinbaseClient(IHttpClientFactory factory, ILogger<CoinbaseClient> logger)
        {
            _http = factory.CreateClient("Coinbase");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            // Coinbase uses dash separator and USD: BTC/USDT -> BTC-USD
            var coinbaseSymbol = symbol.Replace("/USDT", "-USD").Replace("/", "-");
            try
            {
                var response = await _http.GetAsync($"/v2/prices/{coinbaseSymbol}/spot");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var data = doc.RootElement.GetProperty("data");

                return new ExchangePrice
                {
                    Exchange = "Coinbase",
                    Symbol = symbol,
                    Price = decimal.Parse(data.GetProperty("amount").GetString()!),
                    Volume24h = 0, // Coinbase spot endpoint does not return volume
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Coinbase fetch failed for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }
    }
}
