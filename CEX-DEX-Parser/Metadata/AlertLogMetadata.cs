using System.ComponentModel.DataAnnotations;

namespace CEX_DEX_Parser.Metadata
{
    public class AlertLogMetadata
    {
        [Display(Name = "Alert ID")]
        [Required(ErrorMessage = "Alert ID is required.")]
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Symbol")]
        [Required(ErrorMessage = "Symbol is required.")]
        [StringLength(20, ErrorMessage = "Symbol cannot exceed 20 characters.")]
        public string Symbol { get; set; } = string.Empty;

        [Display(Name = "Highest Exchange")]
        [Required(ErrorMessage = "High exchange is required.")]
        [StringLength(50, ErrorMessage = "Exchange name cannot exceed 50 characters.")]
        public string HighExchange { get; set; } = string.Empty;

        [Display(Name = "Lowest Exchange")]
        [Required(ErrorMessage = "Low exchange is required.")]
        [StringLength(50, ErrorMessage = "Exchange name cannot exceed 50 characters.")]
        public string LowExchange { get; set; } = string.Empty;

        [Display(Name = "Highest Price")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal HighPrice { get; set; }

        [Display(Name = "Lowest Price")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal LowPrice { get; set; }

        [Display(Name = "Spread Percentage")]
        [Range(0, 100, ErrorMessage = "Spread percentage must be between 0 and 100.")]
        public decimal SpreadPercent { get; set; }

        [Display(Name = "Triggered At")]
        [Required(ErrorMessage = "Triggered timestamp is required.")]
        [DataType(DataType.DateTime)]
        public DateTime TriggeredAt { get; set; }
    }
}
