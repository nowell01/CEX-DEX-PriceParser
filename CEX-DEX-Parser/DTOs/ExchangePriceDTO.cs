using CEX_DEX_Parser.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.DTOs
{
    [ModelMetadataType(typeof(ExchangePriceMetadata))]
    public class ExchangePriceDTO
    {
        public string? Exchange { get; set; }
        public string? Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal Volume24h { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
