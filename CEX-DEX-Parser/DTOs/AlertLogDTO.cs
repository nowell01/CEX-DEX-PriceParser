using CEX_DEX_Parser.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.DTOs
{
    [ModelMetadataType(typeof(AlertLogMetadata))]
    public class AlertLogDTO
    {
        public string? Id { get; set; }
        public string? Symbol { get; set; }
        public string? HighExchange { get; set; }
        public string? LowExchange { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal SpreadPercent { get; set; }
        public DateTime TriggeredAt { get; set; }
    }
}
