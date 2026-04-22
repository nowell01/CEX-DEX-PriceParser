using System.ComponentModel.DataAnnotations;

namespace CEX_DEX_Parser.Metadata
{
    public class PriceComparisonMetadata
    {
        [Display(Name = "Symbol")]
        [Required(ErrorMessage = "Symbol is required.")]
        [StringLength(20, ErrorMessage = "Symbol cannot exceed 20 characters.")]
        public string Symbol { get; set; } = string.Empty;

        [Display(Name = "Price Spread")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Spread must be a positive value.")]
        public decimal Spread { get; set; }

        [Display(Name = "Spread Percentage")]
        [Range(0, 100, ErrorMessage = "Spread percentage must be between 0 and 100.")]
        public decimal SpreadPercent { get; set; }

        [Display(Name = "Highest Exchange")]
        [StringLength(50, ErrorMessage = "Exchange name cannot exceed 50 characters.")]
        public string HighestExchange { get; set; } = string.Empty;

        [Display(Name = "Lowest Exchange")]
        [StringLength(50, ErrorMessage = "Exchange name cannot exceed 50 characters.")]
        public string LowestExchange { get; set; } = string.Empty;
    }
}
