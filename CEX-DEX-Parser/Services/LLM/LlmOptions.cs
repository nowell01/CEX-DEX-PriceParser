namespace CEX_DEX_Parser.Services.LLM
{
    public sealed class LlmOptions
    {
        public const string Section = "Llm";
        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "openai/gpt-oss-120b";
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.2;
        public string? ReasoningEffort { get; set; } = "low";
        public bool JsonMode { get; set; } = true;
        public int CacheMinutes { get; set; } = 30;
    }

    public sealed class LlmException(string message) : Exception(message);
}