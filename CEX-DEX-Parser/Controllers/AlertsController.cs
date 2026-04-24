using CEX_DEX_Parser.DTOs;
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
