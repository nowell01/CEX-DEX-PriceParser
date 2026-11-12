using System.Text.Json;
using CEX_DEX_Parser.Models;
using CEX_DEX_Parser.Services.LLM;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CEX_DEX_Parser.Services.Insights
{
    public sealed class AlertInsightsService(
    JsonStorageService storage,
    ILlmClient llm,
    IMemoryCache cache,
    IOptions<LlmOptions> options,
    ILogger<AlertInsightsService> logger)
    {
        private static readonly JsonSerializerOptions ParseJson = new(JsonSerializerDefaults.Web);

        public async Task<AlertSummaryResponse> GetSummaryAsync(int hours, CancellationToken ct)
        {
            var cacheKey = $"ai-summary:{hours}";
            if (cache.TryGetValue(cacheKey, out AlertSummaryResponse? cached) && cached is not null)
                return cached with { FromCache = true };

            var to = DateTime.UtcNow;
            var from = to.AddHours(-hours);

            var alerts = (await storage.ReadAsync<AlertLog>("alerts-log.json"))
                .Where(a => a.TriggeredAt >= from)
                .ToList();

            var response = alerts.Count == 0
                ? EmptyWindow(from, to)
                : await SummarizeAsync(alerts, from, to, hours, ct);

            cache.Set(cacheKey, response, TimeSpan.FromMinutes(options.Value.CacheMinutes));
            return response;
        }

        private async Task<AlertSummaryResponse> SummarizeAsync(
            List<AlertLog> alerts, DateTime from, DateTime to, int hours, CancellationToken ct)
        {
            var digest = AlertDigestBuilder.Build(alerts, from, to);
            var result = await llm.CompleteAsync(InsightPrompts.System, InsightPrompts.User(digest, hours), ct);

            if (result.StopReason == "max_tokens")
                logger.LogWarning("AI summary was cut off; consider raising Llm:MaxTokens");

            return new AlertSummaryResponse(from, to, alerts.Count, Parse(result.Text),
                options.Value.Model, result.InputTokens, result.OutputTokens, DateTime.UtcNow);
        }

        private static AlertSummaryResponse EmptyWindow(DateTime from, DateTime to) => new(
            from, to, 0,
            new AlertSummary("No arbitrage alerts in this window.", "quiet",
                "No spreads above 3% were detected.", [], []),
            "none", 0, 0, DateTime.UtcNow);

        private static AlertSummary Parse(string text)
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<AlertSummary>(text[start..(end + 1)], ParseJson);
                    if (parsed is not null)
                        return parsed with
                        {
                            Highlights = parsed.Highlights ?? [],
                            Warnings = parsed.Warnings ?? []
                        };
                }
                catch (JsonException)
                {
                }
            }

            return new AlertSummary("AI summary", "normal", text, [], []);
        }
    }
}