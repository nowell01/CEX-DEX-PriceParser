using System.Text.Json;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class UniswapClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<UniswapClient> _logger;

        // Uniswap is an Ethereum-based DEX. We query DexScreener (no API key required)
        // and filter the returned pairs to only those traded on Uniswap (v2 / v3).
        // ERC-20 contract addresses on Ethereum mainnet:
        private static readonly Dictionary<string, string> EthereumTokenAddresses = new(StringComparer.OrdinalIgnoreCase)
        {
            { "BTC",  "0x2260FAC5E5542a773Aa44fBCfeDf7C193bc2C599" }, // WBTC
            { "ETH",  "0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2" }, // WETH
            { "USDT", "0xdAC17F958D2ee523a2206206994597C13D831ec7" },
            { "USDC", "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48" },
            { "DAI",  "0x6B175474E89094C44Da98b954EedeAC495271d0F" },
            { "LINK", "0x514910771AF9Ca656af840dff83E8264EcF986CA" },
            { "UNI",  "0x1f9840a85d5aF5bf1D1762F925BDADdC4201F984" },
            { "AAVE", "0x7Fc66500c84A76Ad7e9c93437bFc5Ac33E2DDaE9" },
        };

        public UniswapClient(IHttpClientFactory factory, ILogger<UniswapClient> logger)
        {
            _http = factory.CreateClient("Uniswap");
            _logger = logger;
        }

        public async Task<ExchangePrice?> GetPriceAsync(string symbol)
        {
            var baseAsset = symbol.Split('/')[0].ToUpper();

            if (!EthereumTokenAddresses.TryGetValue(baseAsset, out var address))
            {
                _logger.LogWarning("Uniswap: no Ethereum token address mapped for {Asset}", baseAsset);
                return null;
            }

            try
            {
                // DexScreener returns an aggregated list of all pairs across DEXes for a given token.
                var response = await _http.GetAsync($"/latest/dex/tokens/{address}");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                if (!doc.RootElement.TryGetProperty("pairs", out var pairs) || pairs.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Uniswap: DexScreener returned no pairs for {Symbol}", symbol);
                    return null;
                }

                // Filter to Uniswap pairs on Ethereum mainnet, then pick the highest 24h volume pair.
                JsonElement? bestPair = null;
                decimal bestVolume = -1;

                foreach (var pair in pairs.EnumerateArray())
                {
                    var dexId = pair.TryGetProperty("dexId", out var d) ? d.GetString() : null;
                    var chainId = pair.TryGetProperty("chainId", out var c) ? c.GetString() : null;

                    if (dexId == null || !dexId.StartsWith("uniswap", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (chainId != "ethereum")
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
                    _logger.LogWarning("Uniswap: no matching pair found for {Symbol}", symbol);
                    return null;
                }

                var priceUsd = bestPair.Value.GetProperty("priceUsd").GetString();

                return new ExchangePrice
                {
                    Exchange = "Uniswap",
                    Symbol = symbol,
                    Price = decimal.Parse(priceUsd!),
                    Volume24h = bestVolume,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Uniswap fetch failed for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }
    }
}
