namespace CEX_DEX_Parser.Services
{
    public class ChatIdStore
    {
        private readonly JsonStorageService _storage;
        private const string FileName = "telegram-subscribers.json";

        public ChatIdStore(JsonStorageService storage)
        {
            _storage = storage;
        }

        public async void Add(string chatId)
        {
            var existing = await _storage.ReadAsync<string>(FileName);
            if (!existing.Contains(chatId))
            {
                existing.Add(chatId);
                await _storage.WriteAsync(FileName, existing);
            }
        }

        public Task<List<string>> GetAllAsync() =>
            _storage.ReadAsync<string>(FileName);
    }
}
