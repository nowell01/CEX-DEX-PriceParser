using CEX_DEX_Parser.Models;
using CEX_DEX_Parser.Services;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.Controllers
{
    [ApiController]
    [Route("api/prices")]
    public class PricesController : ControllerBase
    {
        private readonly ExchangeService _exchangeService;
        private readonly JsonStorageService _storage;

        public PricesController(ExchangeService exchangeService, JsonStorageService storage)
        {
            _exchangeService = exchangeService;
            _storage = storage;
        }

        [HttpGet]
        public async Task<ActionResult<List<PriceComparison>>> GetAll()
        {
            var watchlist = await _storage.ReadAsync<TradingPair>("watchlist.json");
            var symbols = watchlist.Count > 0
                ? watchlist.Select(p => p.Symbol)
                : new[] { "BTC/USDT", "ETH/USDT" }; // defaults if watchlist is empty

            var comparisons = await _exchangeService.GetAllComparisonsAsync(symbols);
            return Ok(comparisons);
        }

        [HttpGet("{symbol}")]
        public async Task<ActionResult<PriceComparison>> GetBySymbol(string symbol)
        {
            var decoded = Uri.UnescapeDataString(symbol);
            var comparison = await _exchangeService.GetComparisonAsync(decoded);
            if (comparison == null)
                return NotFound($"Could not fetch prices for {decoded}");

            return Ok(comparison);
        }
    }
}
