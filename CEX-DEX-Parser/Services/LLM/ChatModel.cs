namespace CEX_DEX_Parser.Services.LLM
{
    public record ChatRequest(
        string Model,
        List<ChatMessage> Messages,
        int MaxTokens,
        double? Temperature = null,
        string? ReasoningEffort = null,
        ChatResponseFormat? ResponseFormat = null);

    public record ChatMessage(string Role, string? Content);
    public record ChatResponseFormat(string Type);

    public record ChatResponse(string? Id, string? Model, List<ChatChoice> Choices, ChatUsage? Usage);
    public record ChatChoice(int Index, ChatMessage Message, string? FinishReason);
    public record ChatUsage(int PromptTokens, int CompletionTokens, int TotalTokens);

    public record LlmResult(string Text, string? StopReason, int InputTokens, int OutputTokens);
}