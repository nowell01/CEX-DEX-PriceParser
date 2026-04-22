using System.ComponentModel.DataAnnotations;

namespace CEX_DEX_Parser.Metadata
{
    public class TradingPairMetadata
    {
        [Display(Name = "Trading Pair Symbol")]
        [Required(ErrorMessage = "Symbol is required.")]
        [StringLength(20, ErrorMessage = "Symbol cannot exceed 20 characters.")]
        [RegularExpression(@"^[A-Z0-9]+/[A-Z0-9]+$", ErrorMessage = "Symbol must be in format BASE/QUOTE (e.g. BTC/USDT).")]
        public string Symbol { get; set; } = string.Empty;

        [Display(Name = "Base Asset")]
        [StringLength(10, ErrorMessage = "Base asset cannot exceed 10 characters.")]
        public string BaseAsset { get; set; } = string.Empty;

        [Display(Name = "Quote Asset")]
        [StringLength(10, ErrorMessage = "Quote asset cannot exceed 10 characters.")]
        public string QuoteAsset { get; set; } = string.Empty;
    }
}
