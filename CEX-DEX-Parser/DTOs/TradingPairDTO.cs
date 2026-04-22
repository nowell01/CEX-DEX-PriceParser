using CEX_DEX_Parser.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.DTOs
{
    [ModelMetadataType(typeof(TradingPairMetadata))]
    public class TradingPairDTO
    {
        public string? Symbol { get; set; }
        public string? BaseAsset { get; set; }
        public string? QuoteAsset { get; set; }
    }
}
