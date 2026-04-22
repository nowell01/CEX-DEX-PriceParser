using CEX_DEX_Parser.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.DTOs
{
    [ModelMetadataType(typeof(PriceComparisonMetadata))]
    public class PriceComparisonDTO
    {
        public string? Symbol { get; set; }
        public List<ExchangePriceDTO> Prices { get; set; } = new();
        public decimal Spread { get; set; }
        public decimal SpreadPercent { get; set; }
        public string? HighestExchange { get; set; }
        public string? LowestExchange { get; set; }
    }
}
