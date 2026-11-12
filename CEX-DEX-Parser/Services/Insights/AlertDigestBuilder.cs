using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services.Insights
{
    public record AlertDigest(DateTime FromUtc, DateTime ToUtc, int TotalAlerts,
        List<SymbolStat> BySymbol, List<RouteStat> TopRoutes, List<AlertSample> LargestSpreads);

    public record SymbolStat(string Symbol, int Count, decimal AvgSpread, decimal MaxSpread);

    public record RouteStat(string Symbol, string BuyOn, string SellOn, int Count,
        decimal AvgSpread, List<string> Flags);

    public record AlertSample(string Symbol, string BuyOn, decimal BuyPrice, string SellOn,
        decimal SellPrice, decimal SpreadPct, DateTime Time);

    public static class AlertDigestBuilder
    {
        public static AlertDigest Build(IReadOnlyList<AlertLog> alerts, DateTime from, DateTime to)
        {
            var bySymbol = alerts
                .GroupBy(a => a.Symbol)
                .Select(g => new SymbolStat(
                    g.Key,
                    g.Count(),
                    Math.Round(g.Average(a => a.SpreadPercent), 2),
                    Math.Round(g.Max(a => a.SpreadPercent), 2)))
                .OrderByDescending(s => s.Count)
                .Take(15)
                .ToList();

            var routes = alerts
                .GroupBy(a => (a.Symbol, a.LowExchange, a.HighExchange))
                .Select(g => new RouteStat(
                    g.Key.Symbol,
                    g.Key.LowExchange,
                    g.Key.HighExchange,
                    g.Count(),
                    Math.Round(g.Average(a => a.SpreadPercent), 2),
                    Flags(g.Key.LowExchange, g.Key.HighExchange, g.Max(a => a.SpreadPercent), g.Count())))
                .OrderByDescending(r => r.Count)
                .Take(15)
                .ToList();

            var largest = alerts
                .OrderByDescending(a => a.SpreadPercent)
                .Take(10)
                .Select(a => new AlertSample(a.Symbol, a.LowExchange, a.LowPrice, a.HighExchange,
                    a.HighPrice, Math.Round(a.SpreadPercent, 2), a.TriggeredAt))
                .ToList();

            return new AlertDigest(from, to, alerts.Count, bySymbol, routes, largest);
        }

        private static List<string> Flags(string buy, string sell, decimal maxSpread, int count)
        {
            var flags = new List<string>();
            if (maxSpread > 15)
                flags.Add("EXTREME_SPREAD: likely bad data or wrong token mapping");
            if (buy == "Hyperliquid" || sell == "Hyperliquid")
                flags.Add("PERP_VS_SPOT: includes basis/funding, not pure arbitrage");
            if (buy == "PancakeSwap" || sell == "PancakeSwap")
                flags.Add("DEX_LEG: check pool liquidity, slippage and gas");
            if (count >= 6)
                flags.Add("PERSISTENT: spread not closing, deposits/withdrawals may be suspended");
            return flags;
        }
    }
}