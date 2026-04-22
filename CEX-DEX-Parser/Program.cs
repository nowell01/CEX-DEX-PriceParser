using CEX_DEX_Parser.Hubs;
using CEX_DEX_Parser.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — allow React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // required for SignalR
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
builder.Services.AddHttpClient("Uniswap", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExchangeApis:Uniswap"]!);
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

// Application services
builder.Services.AddSingleton<JsonStorageService>();
builder.Services.AddScoped<BinanceClient>();
builder.Services.AddScoped<CoinbaseClient>();
builder.Services.AddScoped<KuCoinClient>();
builder.Services.AddScoped<UniswapClient>();
builder.Services.AddScoped<PancakeSwapClient>();
builder.Services.AddScoped<HyperliquidClient>();
builder.Services.AddScoped<ExchangeService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddSingleton<ArbitrageDetector>();
builder.Services.AddHostedService<PriceMonitorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AlertHub>("/hubs/alerts");

app.Run();
