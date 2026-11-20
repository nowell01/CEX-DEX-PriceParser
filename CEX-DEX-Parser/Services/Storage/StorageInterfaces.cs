using CEX_DEX_Parser.Models;

namespace CEX_DEX_Parser.Services.Storage
{
    public class StorageInterfaces
    {
        public interface IChatIdStore
        {
            Task AddAsync(string chatId);
            Task<List<string>> GetAllAsync();
        }

        public interface IAlertLogStore
        {
            Task AddAsync(AlertLog alert);
            Task<List<AlertLog>> GetRecentAsync(int take = 100);
        }

        public interface IPriceHistoryStore
        {
            Task AddAsync(PriceComparison comparison);
            Task AddManyAsync(IEnumerable<PriceComparison> comparisons);
            Task<List<PriceComparison>> GetHistoryAsync(string symbol, DateTime fromUtc, DateTime toUtc);
        }
    }
}
