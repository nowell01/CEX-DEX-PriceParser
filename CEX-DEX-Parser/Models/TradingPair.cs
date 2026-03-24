namespace CEX_DEX_Parser.Models
{
    public class TradingPair
    {
        public string Symbol { get; set; } = string.Empty;
        public string BaseAsset { get; set; } = string.Empty;
        public string QuoteAsset { get; set; } = string.Empty;
    }
}
