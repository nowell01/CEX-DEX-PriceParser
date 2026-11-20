using CEX_DEX_Parser.Data.Entities;
using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services.Storage
{
    public static class PriceMapper
    {
        public static DateTime Utc(DateTime d) => d.Kind switch
        {
            DateTimeKind.Utc => d,
            DateTimeKind.Local => d.ToUniversalTime(),
            _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
        };

        /// <param name="capturedAt">Scan time. If null, uses the latest exchange timestamp (for imported history).</param>
        public static PriceSnapshot ToSnapshot(PriceComparison c, DateTime? capturedAt = null) => new()
        {
            Symbol = c.Symbol,
            CapturedAt = Utc(capturedAt ?? c.Prices.Max(p => p.Timestamp)),
            Spread = c.Spread,
            SpreadPercent = c.SpreadPercent,
            HighestExchange = c.HighestExchange,
            LowestExchange = c.LowestExchange,
            Prices = c.Prices.Select(p => new ExchangePriceRow
            {
                Exchange = p.Exchange,
                Symbol = p.Symbol,
                Price = p.Price,
                Volume24h = p.Volume24h,
                Timestamp = Utc(p.Timestamp)
            }).ToList()
        };
    }
}