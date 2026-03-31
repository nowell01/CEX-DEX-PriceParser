using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class ArbitrageDetector
    {
        public List<AlertLog> Check(IEnumerable<PriceComparison> comparisons, IEnumerable<AlertConfig> configs)
        {
            var triggered = new List<AlertLog>();
            var configMap = configs
                .Where(c => c.IsEnabled)
                .ToDictionary(c => c.Symbol, StringComparer.OrdinalIgnoreCase);

            foreach (var comparison in comparisons)
            {
                if (!configMap.TryGetValue(comparison.Symbol, out var config))
                    continue;

                if (comparison.SpreadPercent >= config.ThresholdPercent)
                {
                    triggered.Add(new AlertLog
                    {
                        Id = Guid.NewGuid().ToString(),
                        Symbol = comparison.Symbol,
                        HighExchange = comparison.HighestExchange,
                        LowExchange = comparison.LowestExchange,
                        HighPrice = comparison.Prices.Max(p => p.Price),
                        LowPrice = comparison.Prices.Min(p => p.Price),
                        SpreadPercent = comparison.SpreadPercent,
                        TriggeredAt = DateTime.UtcNow
                    });
                }
            }

            return triggered;
        }
    }
}
