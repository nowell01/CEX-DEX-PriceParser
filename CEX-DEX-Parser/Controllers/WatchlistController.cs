using CEX_DEX_Parser.DTOs;
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

        // GET: api/watchlist
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TradingPairDTO>>> GetAll()
        {
            var pairs = await _storage.ReadAsync<TradingPair>("watchlist.json");

            var pairDTOs = pairs
                .Select(p => new TradingPairDTO
                {
                    Symbol = p.Symbol,
                    BaseAsset = p.BaseAsset,
                    QuoteAsset = p.QuoteAsset
                })
                .ToList();

            if (pairDTOs.Count > 0)
            {
                return pairDTOs;
            }
            else
            {
                return NotFound(new { message = "Error: No trading pairs found in the watchlist." });
            }
        }

        // POST: api/watchlist
        [HttpPost]
        public async Task<ActionResult<TradingPairDTO>> Add([FromBody] TradingPairDTO pairDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            TradingPair pair = new TradingPair
            {
                Symbol = pairDTO.Symbol!.ToUpper().Trim()
            };

            var parts = pair.Symbol.Split('/');
            if (parts.Length == 2)
            {
                pair.BaseAsset = parts[0];
                pair.QuoteAsset = parts[1];
            }

            var pairs = await _storage.ReadAsync<TradingPair>("watchlist.json");

            if (pairs.Any(p => string.Equals(p.Symbol, pair.Symbol, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new { message = $"Unable to save: {pair.Symbol} is already in the watchlist." });
            }

            pairs.Add(pair);
            await _storage.WriteAsync("watchlist.json", pairs);

            pairDTO.Symbol = pair.Symbol;
            pairDTO.BaseAsset = pair.BaseAsset;
            pairDTO.QuoteAsset = pair.QuoteAsset;

            return CreatedAtAction(nameof(GetAll), pairDTO);
        }

        // DELETE: api/watchlist/{symbol}
        [HttpDelete("{symbol}")]
        public async Task<IActionResult> Remove(string symbol)
        {
            var decoded = Uri.UnescapeDataString(symbol).ToUpper();
            var pairs = await _storage.ReadAsync<TradingPair>("watchlist.json");
            var removed = pairs.RemoveAll(p =>
                string.Equals(p.Symbol, decoded, StringComparison.OrdinalIgnoreCase));

            if (removed == 0)
            {
                return NotFound(new { message = $"Error: {decoded} not found in watchlist." });
            }

            await _storage.WriteAsync("watchlist.json", pairs);
            return NoContent();
        }
    }
}
