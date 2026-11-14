using CEX_DEX_Parser.Hubs;
using CEX_DEX_Parser.Services;
using Telegram.Bot;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using CEX_DEX_Parser.Services.LLM;
using CEX_DEX_Parser.Services.Insights;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — allow React dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "https://cex-dex-client.vercel.app",
                "https://cex-dex-client-git-main-nowell01s-projects.vercel.app",
                "https://www.cryptoscan.tech",
                "https://cryptoscan.tech"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

// Named HttpClients for each exchange
builder.Services.AddHttpClient("Binance", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExchangeApis:Binance"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("Coinbase", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExchangeApis:Coinbase"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("KuCoin", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExchangeApis:KuCoin"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("Bybit", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExchangeApis:Bybit"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("OKX", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExchangeApis:OKX"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("PancakeSwap", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExchangeApis:PancakeSwap"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("Hyperliquid", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExchangeApis:Hyperliquid"]!);
    c.Timeout = TimeSpan.FromSeconds(10);
});

//LLM API calls configuration
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.Section));

builder.Services.AddHttpClient<ILlmClient, OpenAiCompatibleClient>((sp, http) =>
{
    var o = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
    http.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
    if (!string.IsNullOrWhiteSpace(o.ApiKey))
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);
    http.Timeout = TimeSpan.FromMinutes(3);
})
.AddStandardResilienceHandler(r =>
{
    r.Retry.MaxRetryAttempts = 2;
    r.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
    r.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(150);
    r.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("llm", w =>
    {
        w.PermitLimit = 2;
        w.Window = TimeSpan.FromMinutes(1);
        w.QueueLimit = 0;
    });
});

// Application services
builder.Services.AddSingleton<JsonStorageService>();
builder.Services.AddScoped<BinanceClient>();
builder.Services.AddScoped<CoinbaseClient>();
builder.Services.AddScoped<KuCoinClient>();
builder.Services.AddScoped<BybitClient>();
builder.Services.AddScoped<OkxClient>();
builder.Services.AddScoped<PancakeSwapClient>();
builder.Services.AddScoped<HyperliquidClient>();
builder.Services.AddScoped<ExchangeService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddSingleton<ArbitrageDetector>();
builder.Services.AddSingleton<ITelegramNotifier, TelegramNotifier>();
builder.Services.AddSingleton<ChatIdStore>();
builder.Services.AddHostedService<PriceMonitorService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<AlertInsightsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReact");
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.MapControllers();
// Register webhook with Telegram on startup
var bot = app.Services.GetRequiredService<ITelegramNotifier>();
var botClient = new TelegramBotClient(app.Configuration["Telegram:BotToken"]!);
await botClient.SetWebhook(
    url: "https://cex-dex-parser-h7b3f0gwbyfah7ft.canadacentral-01.azurewebsites.net/api/telegram/webhook"
);
app.MapHub<AlertHub>("/hubs/alerts");

app.Run();
