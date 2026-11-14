using CEX_DEX_Parser.Services.Insights;
using CEX_DEX_Parser.Services.LLM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CEX_DEX_Parser.Controllers 
{
    [ApiController]
    [Route("api/insights")]
    public class InsightsController(AlertInsightsService insights, ILogger<InsightsController> logger)
        : ControllerBase
    {
        [HttpGet("summary")]
        [EnableRateLimiting("llm")]
        public async Task<IActionResult> Summary([FromQuery] int hours = 24, CancellationToken ct = default)
        {
            hours = Math.Clamp(hours, 1, 336);

            try
            {
                return Ok(await insights.GetSummaryAsync(hours, ct));
            }
            catch (LlmException ex)
            {
                logger.LogWarning(ex, "AI summary failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { error = "AI summary temporarily unavailable" });
            }
        }
    }
}