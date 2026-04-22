using CEX_DEX_Parser.DTOs;
using CEX_DEX_Parser.Models;
using CEX_DEX_Parser.Services;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ControllerBase
    {
        private readonly AlertService _alertService;

        public AlertsController(AlertService alertService)
        {
            _alertService = alertService;
        }

        // GET: api/alerts/config
        [HttpGet("config")]
        public async Task<ActionResult<IEnumerable<AlertConfigDTO>>> GetConfigs()
        {
            var configs = await _alertService.GetConfigsAsync();

            var configDTOs = configs
                .Select(c => new AlertConfigDTO
                {
                    Symbol = c.Symbol,
                    ThresholdPercent = c.ThresholdPercent,
                    IsEnabled = c.IsEnabled
                })
                .ToList();

            if (configDTOs.Count > 0)
            {
                return configDTOs;
            }
            else
            {
                return NotFound(new { message = "Error: No alert configurations found." });
            }
        }

        // POST: api/alerts/config
        [HttpPost("config")]
        public async Task<ActionResult<AlertConfigDTO>> PostConfig(AlertConfigDTO configDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AlertConfig config = new AlertConfig
            {
                Symbol = configDTO.Symbol!.ToUpper().Trim(),
                ThresholdPercent = configDTO.ThresholdPercent,
                IsEnabled = configDTO.IsEnabled
            };

            await _alertService.SaveConfigAsync(config);

            configDTO.Symbol = config.Symbol;

            return Ok(configDTO);
        }

        // DELETE: api/alerts/config/{symbol}
        [HttpDelete("config/{symbol}")]
        public async Task<IActionResult> DeleteConfig(string symbol)
        {
            var decoded = Uri.UnescapeDataString(symbol).ToUpper();
            await _alertService.DeleteConfigAsync(decoded);
            return NoContent();
        }

        // GET: api/alerts/history
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<AlertLogDTO>>> GetHistory()
        {
            var history = await _alertService.GetHistoryAsync();

            var historyDTOs = history
                .OrderByDescending(a => a.TriggeredAt)
                .Select(a => new AlertLogDTO
                {
                    Id = a.Id,
                    Symbol = a.Symbol,
                    HighExchange = a.HighExchange,
                    LowExchange = a.LowExchange,
                    HighPrice = a.HighPrice,
                    LowPrice = a.LowPrice,
                    SpreadPercent = a.SpreadPercent,
                    TriggeredAt = a.TriggeredAt
                })
                .ToList();

            if (historyDTOs.Count > 0)
            {
                return historyDTOs;
            }
            else
            {
                return NotFound(new { message = "Error: No alert history found." });
            }
        }
    }
}
