using System.Text.Json;

namespace CEX_DEX_Parser.Services
{
    public class JsonStorageService
    {
        private readonly string _dataDir;
        private readonly JsonSerializerOptions _options;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public JsonStorageService(IWebHostEnvironment env)
        {
            _dataDir = Path.Combine(env.ContentRootPath, "Data");

            if (!Directory.Exists(_dataDir))
                Directory.CreateDirectory(_dataDir);

            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<T>> ReadAsync<T>(string fileName)
        {
            var path = Path.Combine(_dataDir, fileName);

            await _lock.WaitAsync();
            try
            {
                if (!File.Exists(path))
                    return new List<T>();

                var json = await File.ReadAllTextAsync(path);

                if (string.IsNullOrWhiteSpace(json))
                    return new List<T>();

                return JsonSerializer.Deserialize<List<T>>(json, _options) ?? new List<T>();
            }
            catch (JsonException)
            {
                // If the file is corrupted, return empty list
                return new List<T>();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task WriteAsync<T>(string fileName, List<T> data)
        {
            var path = Path.Combine(_dataDir, fileName);

            await _lock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(data, _options);
                await File.WriteAllTextAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AppendAsync<T>(string fileName, T item)
        {
            var list = await ReadAsync<T>(fileName);
            list.Add(item);
            await WriteAsync(fileName, list);
        }
    }
}