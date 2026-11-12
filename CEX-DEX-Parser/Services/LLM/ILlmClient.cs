namespace CEX_DEX_Parser.Services.LLM
{
    public interface ILlmClient
    {
        Task<LlmResult> CompleteAsync(string system, string user, CancellationToken ct);
    }
}