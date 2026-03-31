using CEX_DEX_Parser.Models;
using CEX_DEX_Parser.Services;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.Controllers
{
    [ApiController]
    [Route("api/watchlist")]
    public class WatchlistController : ControllerBase
    {
        private readonly JsonStorageService _storage;

        public WatchlistController(JsonStorageService storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public async Task<ActionResult<List<TradingPair>>> GetAll()
        {
            var pairs = await _storage.ReadAsync<TradingPair>("watchlist.json");
            return Ok(pairs);
        }

        [HttpPost]
        public async Task<ActionResult<TradingPair>> Add([FromBody] TradingPair pair)
        {
            if (string.IsNullOrWhiteSpace(pair.Symbol))
                return BadRequest("Symbol is required.");

            pair.Symbol = pair.Symbol.ToUpper().Trim();

            var parts = pair.Symbol.Split('/');
            if (parts.Length == 2)
            {
                pair.BaseAsset = parts[0];
                pair.QuoteAsset = parts[1];
            }

            var pairs = await _storage.ReadAsync<TradingPair>("watchlist.json");

            if (pairs.Any(p => string.Equals(p.Symbol, pair.Symbol, StringComparison.OrdinalIgnoreCase)))
                return Conflict($"{pair.Symbol} is already in the watchlist.");

            pairs.Add(pair);
            await _storage.WriteAsync("watchlist.json", pairs);
            return CreatedAtAction(nameof(GetAll), pair);
        }

        [HttpDelete("{symbol}")]
        public async Task<IActionResult> Remove(string symbol)
        {
            var decoded = Uri.UnescapeDataString(symbol).ToUpper();
            var pairs = await _storage.ReadAsync<TradingPair>("watchlist.json");
            var removed = pairs.RemoveAll(p =>
                string.Equals(p.Symbol, decoded, StringComparison.OrdinalIgnoreCase));

            if (removed == 0)
                return NotFound($"{decoded} not found in watchlist.");

            await _storage.WriteAsync("watchlist.json", pairs);
            return NoContent();
        }
    }
}
