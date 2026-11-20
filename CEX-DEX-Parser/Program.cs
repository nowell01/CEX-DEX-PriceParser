using CEX_DEX_Parser.Data;
using CEX_DEX_Parser.Hubs;
using CEX_DEX_Parser.Services;
using CEX_DEX_Parser.Services.Insights;
using CEX_DEX_Parser.Services.LLM;
using CEX_DEX_Parser.Services.Storage;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using Telegram.Bot;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL db context connection
builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Db"))
        .UseSnakeCaseNamingConvention());

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
builder.Services.AddSingleton<IChatIdStore, DbChatIdStore>();
builder.Services.AddSingleton<IAlertLogStore, DbAlertLogStore>();
builder.Services.AddSingleton<IPriceHistoryStore, DbPriceHistoryStore>();
builder.Services.AddSingleton<ITelegramNotifier, TelegramNotifier>();
builder.Services.AddSingleton<ArbitrageDetector>();

builder.Services.AddScoped<BinanceClient>();
builder.Services.AddScoped<CoinbaseClient>();
builder.Services.AddScoped<KuCoinClient>();
builder.Services.AddScoped<BybitClient>();
builder.Services.AddScoped<OkxClient>();
builder.Services.AddScoped<PancakeSwapClient>();
builder.Services.AddScoped<HyperliquidClient>();
builder.Services.AddScoped<ExchangeService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<AlertInsightsService>();

builder.Services.AddHostedService<PriceMonitorService>();
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

if (args.Contains("--import-json"))
{
    var idx = Array.IndexOf(args, "--import-json");
    var dir = idx + 1 < args.Length && !args[idx + 1].StartsWith("--")
        ? args[idx + 1]
        : Path.Combine(builder.Environment.ContentRootPath, "data-backup");

    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await using var db = await factory.CreateDbContextAsync();
    await JsonImporter.RunAsync(db, dir, log);
    return;
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
