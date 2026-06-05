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
            //_dataDir = Path.Combine(env.ContentRootPath, "Data");
            var isAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
            _dataDir = isAzure
                ? Path.Combine("D:\\home\\data", "cex-dex-parser")
                : Path.Combine(Path.GetTempPath(), "cex-dex-parser");

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
            //var list = await ReadAsync<T>(fileName);
            //list.Add(item);
            //await WriteAsync(fileName, list);
            var path = Path.Combine(_dataDir, fileName);

            await _lock.WaitAsync();
            try
            {
                List<T> list;

                if (!File.Exists(path))
                {
                    list = new List<T>();
                }
                else
                {
                    var json = await File.ReadAllTextAsync(path);
                    list = string.IsNullOrWhiteSpace(json)
                        ? new List<T>()
                        : JsonSerializer.Deserialize<List<T>>(json, _options) ?? new List<T>();
                }

                list.Add(item);

                var updated = JsonSerializer.Serialize(list, _options);
                await File.WriteAllTextAsync(path, updated);
            }
            catch (JsonException)
            {
                // File corrupted — start fresh with just the new item
                var fallback = JsonSerializer.Serialize(new List<T> { item }, _options);
                await File.WriteAllTextAsync(path, fallback);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}