using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services.Storage
{
    public class StorageInterfaces
    {
        public interface IChatIdStore
        {
            Task<bool> AddAsync(string chatId, CancellationToken ct = default);
            Task<List<string>> GetAllAsync();
        }

        public interface IAlertLogStore
        {
            Task AddAsync(AlertLog alert);
            Task<List<AlertLog>> GetRecentAsync(int take = 100);
            Task<List<AlertLog>> GetSinceAsync(DateTime fromUtc);
        }

        public interface IPriceHistoryStore
        {
            Task AddAsync(PriceComparison comparison);
            Task AddManyAsync(IEnumerable<PriceComparison> comparisons);
            Task<List<PriceComparison>> GetHistoryAsync(string symbol, DateTime fromUtc, DateTime toUtc);
        }
    }
}
