namespace CEX_DEX_Parser.Data.Entities
{
    /// <summary>One scan of one symbol across all exchanges (was one PriceComparison in price-history.json).</summary>
    public class PriceSnapshot
    {
        public long Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public DateTime CapturedAt { get; set; }          // UTC
        public decimal Spread { get; set; }
        public decimal SpreadPercent { get; set; }
        public string HighestExchange { get; set; } = string.Empty;
        public string LowestExchange { get; set; } = string.Empty;

        public List<ExchangePriceRow> Prices { get; set; } = new();
    }
}