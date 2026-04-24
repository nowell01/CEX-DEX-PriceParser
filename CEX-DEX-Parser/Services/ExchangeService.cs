using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class ExchangeService
    {
        private readonly BinanceClient _binance;
        private readonly CoinbaseClient _coinbase;
        private readonly KuCoinClient _kuCoin;
        private readonly BybitClient _bybit;
        private readonly OkxClient _okx;
        private readonly PancakeSwapClient _pancakeSwap;
        private readonly HyperliquidClient _hyperliquid;
        private readonly ILogger<ExchangeService> _logger;

        public ExchangeService(
            BinanceClient binance,
            CoinbaseClient coinbase,
            KuCoinClient kuCoin,
            BybitClient bybit,
            OkxClient okx,
            PancakeSwapClient pancakeSwap,
            HyperliquidClient hyperliquid,
            ILogger<ExchangeService> logger)
        {
            _binance = binance;
            _coinbase = coinbase;
            _kuCoin = kuCoin;
            _bybit = bybit;
            _okx = okx;
            _pancakeSwap = pancakeSwap;
            _hyperliquid = hyperliquid;
            _logger = logger;
        }

        public async Task<PriceComparison?> GetComparisonAsync(string symbol)
        {
            var tasks = new[]
            {
                _binance.GetPriceAsync(symbol),
                _coinbase.GetPriceAsync(symbol),
                _kuCoin.GetPriceAsync(symbol),
                _bybit.GetPriceAsync(symbol),
                _okx.GetPriceAsync(symbol),
                _pancakeSwap.GetPriceAsync(symbol),
                _hyperliquid.GetPriceAsync(symbol)
            };

            var results = await Task.WhenAll(tasks);
            var prices = results.Where(p => p != null).Cast<ExchangePrice>().ToList();

            if (prices.Count < 2)
            {
                _logger.LogWarning("Not enough exchange data for {Symbol} (got {Count})", symbol, prices.Count);
                return null;
            }

            var highest = prices.MaxBy(p => p.Price)!;
            var lowest = prices.MinBy(p => p.Price)!;
            var spread = highest.Price - lowest.Price;
            var spreadPercent = lowest.Price > 0 ? (spread / lowest.Price) * 100 : 0;

            return new PriceComparison
            {
                Symbol = symbol,
                Prices = prices,
                Spread = spread,
                SpreadPercent = Math.Round(spreadPercent, 4),
                HighestExchange = highest.Exchange,
                LowestExchange = lowest.Exchange
            };
        }

        public async Task<List<PriceComparison>> GetAllComparisonsAsync(IEnumerable<string> symbols)
        {
            var tasks = symbols.Select(GetComparisonAsync);
            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null).Cast<PriceComparison>().ToList();
        }
    }
}
