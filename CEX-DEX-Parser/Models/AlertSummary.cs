namespace CEX_DEX_Parser.Models
{
    public record SymbolHighlight(string Symbol, string Insight);

    public record AlertSummary(string Headline, string ActivityLevel, string Summary,
        List<SymbolHighlight> Highlights, List<string> Warnings);

    public record AlertSummaryResponse(DateTime FromUtc, DateTime ToUtc, int AlertCount,
        AlertSummary Summary, string Model, int InputTokens, int OutputTokens,
        DateTime GeneratedAtUtc, bool FromCache = false);
}