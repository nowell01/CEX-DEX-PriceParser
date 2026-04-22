using CEX_DEX_Parser.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.DTOs
{
    [ModelMetadataType(typeof(AlertConfigMetadata))]
    public class AlertConfigDTO
    {
        public string? Symbol { get; set; }
        public decimal ThresholdPercent { get; set; }
        public bool IsEnabled { get; set; }
    }
}
