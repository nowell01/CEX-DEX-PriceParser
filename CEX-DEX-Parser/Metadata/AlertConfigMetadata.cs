using System.ComponentModel.DataAnnotations;

namespace CEX_DEX_Parser.Metadata
{
    public class AlertConfigMetadata
    {
        [Display(Name = "Trading Pair Symbol")]
        [Required(ErrorMessage = "Symbol is required.")]
        [StringLength(20, ErrorMessage = "Symbol cannot exceed 20 characters.")]
        [RegularExpression(@"^[A-Z0-9]+/[A-Z0-9]+$", ErrorMessage = "Symbol must be in format BASE/QUOTE (e.g. BTC/USDT).")]
        public string Symbol { get; set; } = string.Empty;

        [Display(Name = "Threshold Percentage")]
        [Required(ErrorMessage = "Threshold percentage is required.")]
        [Range(0.01, 100, ErrorMessage = "Threshold must be between 0.01 and 100.")]
        public decimal ThresholdPercent { get; set; }

        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; }
    }
}
