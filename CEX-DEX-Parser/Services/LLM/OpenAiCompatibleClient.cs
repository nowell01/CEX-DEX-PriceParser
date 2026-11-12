using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using CEX_DEX_Parser.Services.LLM;

namespace CEX_DEX_Parser.Services.LLM
{

    public sealed class OpenAiCompatibleClient(
    HttpClient http,
    IOptions<LlmOptions> options,
    ILogger<OpenAiCompatibleClient> logger) : ILlmClient
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<LlmResult> CompleteAsync(string system, string user, CancellationToken ct)
        {
            var o = options.Value;

            var request = new ChatRequest(
                Model: o.Model,
                Messages: [new("system", system), new("user", user)],
                MaxTokens: o.MaxTokens,
                Temperature: o.Temperature,
                ReasoningEffort: string.IsNullOrWhiteSpace(o.ReasoningEffort) ? null : o.ReasoningEffort,
                ResponseFormat: o.JsonMode ? new ChatResponseFormat("json_object") : null);

            using var response = await http.PostAsJsonAsync("chat/completions", request, Json, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("LLM call failed {Status}: {Body}", (int)response.StatusCode, body);
                throw new LlmException($"LLM call failed ({(int)response.StatusCode})");
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(Json, ct)
                         ?? throw new LlmException("Empty LLM response");

            var choice = result.Choices.FirstOrDefault()
                         ?? throw new LlmException("LLM returned no choices");

            var stopReason = choice.FinishReason == "length" ? "max_tokens" : choice.FinishReason;

            logger.LogInformation("LLM {Model}: {In} in / {Out} out tokens, finish={Finish}",
                result.Model, result.Usage?.PromptTokens, result.Usage?.CompletionTokens, choice.FinishReason);

            return new LlmResult(
                choice.Message.Content ?? "",
                stopReason,
                result.Usage?.PromptTokens ?? 0,
                result.Usage?.CompletionTokens ?? 0);
        }
    }
}