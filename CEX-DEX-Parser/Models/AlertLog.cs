namespace CEX_DEX_Parser.Models
{
    public class AlertLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Symbol { get; set; } = string.Empty;
        public string HighExchange { get; set; } = string.Empty;
        public string LowExchange { get; set; } = string.Empty;
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal SpreadPercent { get; set; }
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    }
}
