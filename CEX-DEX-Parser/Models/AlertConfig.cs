namespace CEX_DEX_Parser.Models
{
    public class AlertConfig
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal ThresholdPercent { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
