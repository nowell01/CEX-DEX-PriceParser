using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class PancakeSwapClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<PancakeSwapClient> _logger;

        // Mapping of base assets to their token addresses on Binance Smart Chain.
        // PancakeSwap is a BSC-based DEX, so we need BEP-20 contract addresses.
        private static readonly Dictionary<string, string> BscTokenAddresses = new(StringComparer.OrdinalIgnoreCase)
        {
            { "BTC",  "0x7130d2A12B9BCbFAe4f2634d864A1Ee1Ce3Ead9c" }, // BTCB (Binance-pegged BTC)
            { "ETH",  "0x2170Ed0880ac9A755fd29B2688956BD959F933F8" }, // Binance-pegged ETH
            { "BNB",  "0xbb4CdB9CBd36B01bD1cBaEBF2De08d9173bc095c" }, // WBNB
            { "CAKE", "0x0E09FaBB73Bd3Ade0a17ECC321fD13a19e81cE82" }, // PancakeSwap Token
            { "SOL",  "0x570A5D26f7765Ecb712C0924E4De545B89fD43dF" }, // Binance-pegged SOL
            { "ADA",  "0x3EE2200Efb3400fAbB9AacF31297cBdD1d435D47" }, // Binance-pegged ADA
            { "DOT",  "0x7083609fCE4d1d8Dc0C979AAb8c869Ea2C873402" }, // Binance-pegged DOT
            { "XRP",  "0x1D2F0da169ceB9fC7B3144628dB156f3F6c60dBE" }, // Binance-pegged XRP
        };

        public PancakeSwapClient(IHttpClientFactory factory, ILogger<PancakeSwapClient> logger)
        {
            _http = factory.CreateClient("PancakeSwap");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            // PancakeSwap returns prices in USD, so we ignore the quote asset
            var baseAsset = symbol.Split('/')[0].ToUpper();

            if (!BscTokenAddresses.TryGetValue(baseAsset, out var address))
            {
                _logger.LogWarning("PancakeSwap: no BSC token address mapped for {Asset}", baseAsset);
                return null;
            }

            try
            {
                var response = await _http.GetAsync($"/api/v2/tokens/{address}");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var data = doc.RootElement.GetProperty("data");

                return new ExchangePrice
                {
                    Exchange = "PancakeSwap",
                    Symbol = symbol,
                    Price = decimal.Parse(data.GetProperty("price").GetString()!),
                    Volume24h = 0, // tokens endpoint does not return 24h volume
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("PancakeSwap fetch failed for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }
    }
}
