using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class PancakeSwapClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<PancakeSwapClient> _logger;

        // PancakeSwap is a BSC-based DEX.
        // We use DexScreener to fetch prices — filtered to dexId "pancakeswap" on chainId "bsc".
        // BEP-20 token contract addresses on Binance Smart Chain:
        private static readonly Dictionary<string, string> BscTokenAddresses = new(StringComparer.OrdinalIgnoreCase)
        {
            { "BTC",  "0x7130d2A12B9BCbFAe4f2634d864A1Ee1Ce3Ead9c" }, // BTCB (Binance-pegged BTC)
            { "ETH",  "0x2170Ed0880ac9A755fd29B2688956BD959F933F8" }, // Binance-pegged ETH
            { "BNB",  "0xbb4CdB9CBd36B01bD1cBaEBF2De08d9173bc095c" }, // WBNB
            { "SOL",  "0x570A5D26f7765Ecb712C0924E4De545B89fD43dF" }, // Binance-pegged SOL
            { "ADA",  "0x3EE2200Efb3400fAbB9AacF31297cBdD1d435D47" }, // Binance-pegged ADA
            { "DOT",  "0x7083609fCE4d1d8Dc0C979AAb8c869Ea2C873402" }, // Binance-pegged DOT
            { "XRP",  "0x1D2F0da169ceB9fC7B3144628dB156f3F6c60dBE" }, // Binance-pegged XRP
            { "DOGE", "0xbA2aE424d960c26247Dd6c32edC70B295c744C43" }, // Binance-pegged DOGE
            { "LTC",  "0x4338665CBB7B2485A8855A139b75D5e34AB0DB94" }, // Binance-pegged LTC
            { "AVAX", "0x1CE0c2827e2eF14D5C4f29a091d735A204794041" }, // Binance-pegged AVAX
            { "TON",  "0x76A797A59Ba2C17726896976B7B3747BfD1d220f" }, // Toncoin on BSC
            { "AAVE", "0xfb6115445Bff7b52FeB98650C87f44907E58f802" }, // Binance-pegged AAVE
            { "ARB",  "0xa050FFb3eEb8200eEB7F61ce34FF644420FD3522" }, // Arbitrum on BSC
        };

        public PancakeSwapClient(IHttpClientFactory factory, ILogger<PancakeSwapClient> logger)
        {
            _http = factory.CreateClient("PancakeSwap");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            var baseAsset = symbol.Split('/')[0].ToUpper();

            if (!BscTokenAddresses.TryGetValue(baseAsset, out var address))
            {
                _logger.LogWarning("PancakeSwap: no BSC token address mapped for {Asset}", baseAsset);
                return null;
            }

            try
            {
                // DexScreener returns all pairs for a given token across every DEX.
                // We filter to PancakeSwap pairs on BSC and pick the highest volume one.
                var response = await _http.GetAsync($"/latest/dex/tokens/{address}");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                if (!doc.RootElement.TryGetProperty("pairs", out var pairs) ||
                    pairs.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("PancakeSwap: DexScreener returned no pairs for {Symbol}", symbol);
                    return null;
                }

                JsonElement? bestPair = null;
                decimal bestVolume = -1;

                foreach (var pair in pairs.EnumerateArray())
                {
                    var dexId = pair.TryGetProperty("dexId", out var d) ? d.GetString() : null;
                    var chainId = pair.TryGetProperty("chainId", out var c) ? c.GetString() : null;

                    if (dexId == null || !dexId.StartsWith("pancakeswap", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (chainId != "bsc")
                        continue;

                    decimal volume = 0;
                    if (pair.TryGetProperty("volume", out var volEl) &&
                        volEl.TryGetProperty("h24", out var h24))
                    {
                        volume = h24.ValueKind == JsonValueKind.Number ? h24.GetDecimal() : 0;
                    }

                    if (volume > bestVolume)
                    {
                        bestVolume = volume;
                        bestPair = pair;
                    }
                }

                if (bestPair == null)
                {
                    _logger.LogWarning("PancakeSwap: no matching pair found on BSC for {Symbol}", symbol);
                    return null;
                }

                var priceUsd = bestPair.Value.GetProperty("priceUsd").GetString();

                return new ExchangePrice
                {
                    Exchange = "PancakeSwap",
                    Symbol = symbol,
                    Price = decimal.Parse(priceUsd!),
                    Volume24h = bestVolume,
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
