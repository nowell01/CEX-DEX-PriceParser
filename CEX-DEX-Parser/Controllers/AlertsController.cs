using CEX_DEX_Parser.DTOs;
using CEX_DEX_Parser.Services;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ControllerBase
    {
        private const int MaxTake = 1000;
        private readonly AlertService _alertService;

        public AlertsController(AlertService alertService)
        {
            _alertService = alertService;
        }

        // GET: api/alerts/history?take=200
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<AlertLogDTO>>> GetHistory([FromQuery] int take = 200)
        {
            take = Math.Clamp(take, 1, MaxTake);

            var history = await _alertService.GetHistoryAsync(take);   // already newest first

            var historyDTOs = history
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
                return historyDTOs;

            return NotFound(new { message = "Error: No alert history found." });
        }
    }
}