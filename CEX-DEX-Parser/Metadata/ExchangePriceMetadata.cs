using System.ComponentModel.DataAnnotations;

namespace CEX_DEX_Parser.Metadata
{
    public class ExchangePriceMetadata
    {
        [Display(Name = "Exchange")]
        [Required(ErrorMessage = "Exchange name is required.")]
        [StringLength(50, ErrorMessage = "Exchange name cannot exceed 50 characters.")]
        public string Exchange { get; set; } = string.Empty;

        [Display(Name = "Symbol")]
        [Required(ErrorMessage = "Symbol is required.")]
        [StringLength(20, ErrorMessage = "Symbol cannot exceed 20 characters.")]
        public string Symbol { get; set; } = string.Empty;

        [Display(Name = "Price")]
        [Required(ErrorMessage = "Price is required.")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; }

        [Display(Name = "24h Volume")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Volume must be a positive value.")]
        public decimal Volume24h { get; set; }

        [Display(Name = "Timestamp")]
        [Required(ErrorMessage = "Timestamp is required.")]
        [DataType(DataType.DateTime)]
        public DateTime Timestamp { get; set; }
    }
}
