using System.Text.Json;

namespace CEX_DEX_Parser.Services.Insights
{
    public static class InsightPrompts
    {
        private static readonly JsonSerializerOptions DigestJson = new(JsonSerializerDefaults.Web);

        public const string System = """
        You are a crypto market analyst inside an arbitrage monitoring dashboard.
        You receive a pre-computed JSON digest of spread alerts (> 3%) across CEXs and DEXs.

        Rules:
        - Use ONLY numbers from the digest. Never invent prices, volumes or news.
        - Treat flags seriously: a flagged route is usually not an executable opportunity. Explain why.
        - Be concrete: name symbols, exchanges and spreads.
        - This is monitoring commentary, not financial advice. Never tell the user to trade.
        - Reply with ONE JSON object and nothing else, in exactly this shape:
          {
            "headline": "one sentence, max 20 words",
            "activityLevel": "quiet" | "normal" | "busy" | "suspicious",
            "summary": "2-4 sentences",
            "highlights": [ { "symbol": "BTC/USDT", "insight": "..." } ],
            "warnings": [ "..." ]
          }
          Max 5 highlights, max 5 warnings.
        """;

        public static string User(AlertDigest digest, int hours) => $"""
        Summarize arbitrage alert activity for the last {hours} hour(s).

        <digest>
        {JsonSerializer.Serialize(digest, DigestJson)}
        </digest>
        """;
    }
}