namespace CEX_DEX_Parser.Models
{
    public class ExchangePrice
    {
        public string Exchange { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Volume24h { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
