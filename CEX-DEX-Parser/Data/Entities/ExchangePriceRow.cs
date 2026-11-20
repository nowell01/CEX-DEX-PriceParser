namespace CEX_DEX_Parser.Data.Entities
{
    /// <summary>One exchange's price inside a snapshot (was one ExchangePrice).</summary>
    public class ExchangePriceRow
    {
        public long Id { get; set; }
        public long SnapshotId { get; set; }
        public PriceSnapshot Snapshot { get; set; } = null!;

        public string Exchange { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Volume24h { get; set; }
        public DateTime Timestamp { get; set; }           // UTC
    }
}