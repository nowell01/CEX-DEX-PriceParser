using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services
{
    public class ArbitrageDetector
    {
        // Fixed threshold: alert when spread exceeds 3%
        private const decimal SpreadThresholdPercent = 3.0m;

        public List<AlertLog> Check(IEnumerable<PriceComparison> comparisons)
        {
            var triggered = new List<AlertLog>();

            foreach (var comparison in comparisons)
            {
                if (comparison.SpreadPercent >= SpreadThresholdPercent)
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
