using CEX_DEX_Parser.DTOs;
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

        // GET: api/prices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PriceComparisonDTO>>> GetAll()
        {
            var watchlist = await _storage.ReadAsync<TradingPair>("watchlist.json");
            var symbols = watchlist.Count > 0
                ? watchlist.Select(p => p.Symbol)
                : new[] { "BTC/USDT", "ETH/USDT" };

            var comparisons = await _exchangeService.GetAllComparisonsAsync(symbols);

            var comparisonDTOs = comparisons
                .Select(c => new PriceComparisonDTO
                {
                    Symbol = c.Symbol,
                    Spread = c.Spread,
                    SpreadPercent = c.SpreadPercent,
                    HighestExchange = c.HighestExchange,
                    LowestExchange = c.LowestExchange,
                    Prices = c.Prices.Select(p => new ExchangePriceDTO
                    {
                        Exchange = p.Exchange,
                        Symbol = p.Symbol,
                        Price = p.Price,
                        Volume24h = p.Volume24h,
                        Timestamp = p.Timestamp
                    }).ToList()
                })
                .ToList();

            if (comparisonDTOs.Count > 0)
            {
                return comparisonDTOs;
            }
            else
            {
                return NotFound(new { message = "Error: No price data available." });
            }
        }

        // GET: api/prices/{symbol}
        [HttpGet("{symbol}")]
        public async Task<ActionResult<PriceComparisonDTO>> GetBySymbol(string symbol)
        {
            var decoded = Uri.UnescapeDataString(symbol);
            var comparison = await _exchangeService.GetComparisonAsync(decoded);

            if (comparison == null)
            {
                return NotFound(new { message = $"Error: Could not fetch prices for {decoded}." });
            }

            var comparisonDTO = new PriceComparisonDTO
            {
                Symbol = comparison.Symbol,
                Spread = comparison.Spread,
                SpreadPercent = comparison.SpreadPercent,
                HighestExchange = comparison.HighestExchange,
                LowestExchange = comparison.LowestExchange,
                Prices = comparison.Prices.Select(p => new ExchangePriceDTO
                {
                    Exchange = p.Exchange,
                    Symbol = p.Symbol,
                    Price = p.Price,
                    Volume24h = p.Volume24h,
                    Timestamp = p.Timestamp
                }).ToList()
            };

            return comparisonDTO;
        }
    }
}
