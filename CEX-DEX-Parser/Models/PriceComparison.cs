namespace CEX_DEX_Parser.Models
{
    public class PriceComparison
    {
        public string Symbol { get; set; } = string.Empty;
        public List<ExchangePrice> Prices { get; set; } = new();
        public decimal Spread { get; set; }
        public decimal SpreadPercent { get; set; }
        public string HighestExchange { get; set; } = string.Empty;
        public string LowestExchange { get; set; } = string.Empty;
    }
}
