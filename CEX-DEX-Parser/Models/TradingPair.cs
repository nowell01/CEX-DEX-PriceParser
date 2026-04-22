using System.ComponentModel.DataAnnotations;
using CEX_DEX_Parser.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.Models
{
    [ModelMetadataType(typeof(TradingPairMetadata))]
    public class TradingPair : IValidatableObject
    {
        public string Symbol { get; set; } = string.Empty;
        public string BaseAsset { get; set; } = string.Empty;
        public string QuoteAsset { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Symbol) && !Symbol.Contains('/'))
            {
                yield return new ValidationResult(
                    "Symbol must contain a '/' separator (e.g. BTC/USDT).",
                    new[] { nameof(Symbol) });
            }
        }
    }
}
