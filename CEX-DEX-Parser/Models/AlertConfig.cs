using System.ComponentModel.DataAnnotations;
using CEX_DEX_Parser.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.Models
{
    [ModelMetadataType(typeof(AlertConfigMetadata))]
    public class AlertConfig : IValidatableObject
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal ThresholdPercent { get; set; }
        public bool IsEnabled { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ThresholdPercent <= 0)
            {
                yield return new ValidationResult(
                    "Threshold percentage must be greater than 0.",
                    new[] { nameof(ThresholdPercent) });
            }
        }
    }
}
