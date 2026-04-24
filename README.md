[README.md](https://github.com/user-attachments/files/27055032/README.md)
# CryptoScan (CEX-DEX-Parser)

A real-time cryptocurrency arbitrage detection system built with **ASP.NET Core (.NET 9) Web API**. The application compares live prices across **7 exchanges** (5 CEX + 2 DEX), detects arbitrage opportunities when the price spread exceeds **3%**, and pushes real-time alerts to connected clients via **SignalR WebSocket**.

---

## Technology Stack

- **.NET 9**, ASP.NET Core Web API
- **SignalR** — WebSocket real-time communication
- **IHttpClientFactory** — Named HTTP clients for each exchange
- **JSON file storage** — Thread-safe persistence with `SemaphoreSlim` locking
- **Swagger / OpenAPI** — Auto-generated API documentation
- **React.js / CSS** - Front-end

---

## Project Architecture

The project follows an **MVC-style separation** using Models, DTOs, Metadata, Controllers, and Services:

- **Models** — Internal domain objects (`ExchangePrice`, `PriceComparison`, `AlertLog`)
- **DTOs** — Data Transfer Objects exposed through the API (decouples API contract from internals)
- **Metadata** — `DataAnnotation` validation rules shared between Model and DTO via `[ModelMetadataType]` attribute (buddy class pattern)
- **Controllers** — API endpoints that map Models to DTOs using LINQ projections
- **Services** — Business logic, exchange API clients, background jobs

### Folder Structure

```
CEX-DEX-Parser/
├── Controllers/          API endpoints (PricesController, AlertsController)
├── DTOs/                 Data Transfer Objects for API responses
├── Hubs/                 SignalR hub for real-time alert push
├── Metadata/             Validation attributes shared by Models & DTOs
├── Models/               Internal domain models
├── Services/             Business logic, exchange clients, background jobs
├── Data/                 JSON storage files (auto-created at runtime)
├── Program.cs            Application entry point & DI configuration
└── appsettings.json      Exchange API base URLs
```

---

## Exchanges Integrated (7 total)

| # | Exchange | Type | API Endpoint | Symbol Format |
|---|----------|------|-------------|---------------|
| 1 | **Binance** | CEX | `GET /api/v3/ticker/24hr?symbol=BTCUSDT` | Concatenated (`BTCUSDT`) |
| 2 | **Coinbase** | CEX | `GET /v2/prices/BTC-USD/spot` | Dash + USD (`BTC-USD`) |
| 3 | **KuCoin** | CEX | `GET /api/v1/market/orderbook/level1?symbol=BTC-USDT` | Dash (`BTC-USDT`) |
| 4 | **Bybit** | CEX | `GET /v5/market/tickers?category=spot&symbol=BTCUSDT` | Concatenated (`BTCUSDT`) |
| 5 | **OKX** | CEX | `GET /api/v5/market/ticker?instId=BTC-USDT` | Dash (`BTC-USDT`) |
| 6 | **PancakeSwap** | DEX | DexScreener: `GET /latest/dex/tokens/{address}` | BEP-20 token address |
| 7 | **Hyperliquid** | DEX | `POST /info` with `{"type":"allMids"}` | Base asset name (`BTC`) |

### Exchange Client Details

**BinanceClient** — Calls Binance's 24hr ticker endpoint. Returns last price and 24h quote volume. No authentication required.

**CoinbaseClient** — Calls Coinbase spot price endpoint. Maps `USDT` pairs to `USD` since Coinbase doesn't support USDT natively. Volume not available from this endpoint.

**KuCoinClient** — Reads the order book level 1 (best bid/ask). Checks for null data in case a token is not listed on KuCoin.

**BybitClient** — Uses Bybit V5 unified API for spot market tickers. Returns price and 24h volume.

**OkxClient** — Calls OKX ticker endpoint. Extracts `last` price and `volCcy24h` (24h volume in quote currency).

**PancakeSwapClient** — Uses DexScreener API (not PancakeSwap directly). Looks up BEP-20 token contract addresses on Binance Smart Chain, queries DexScreener for all trading pairs, filters to `dexId == "pancakeswap"` and `chainId == "bsc"`, and picks the pair with the highest 24h volume. Contains a hardcoded dictionary of 13 BSC token addresses.

**HyperliquidClient** — Sends a POST request to Hyperliquid's info API. Returns a flat object with all listed assets and their mid-market prices. No token address mapping needed — uses the base asset name directly. These are perpetual futures prices, not spot.

---

## Trading Pairs Tracked (21)

```
BTC/USDT    ETH/USDT    SOL/USDT    XRP/USDT    SUI/USDT    DOGE/USDT    ADA/USDT
LTC/USDT    AVAX/USDT   TON/USDT    AAVE/USDT   APEX/USDT   ARB/USDT     BNB/USDT
STRK/USDT   TRX/USDT    PEPE/USDT   LINK/USDT   TRUMP/USDT  SHIB/USDT    XLM/USDT
```

> Not all exchanges support every pair. If an exchange doesn't list a token, its client returns `null` and the system continues with the remaining exchanges (minimum 2 required for a valid comparison).

---

## Program.cs — Entry Point & Dependency Injection

`Program.cs` is the application's composition root. It registers all services using ASP.NET Core's built-in DI container.

### Named HttpClients

Each exchange gets its own named `HttpClient` with a pre-configured base URL and 10-second timeout. This avoids socket exhaustion and provides clean separation of exchange-specific configuration.

```
"Binance"     → https://api.binance.com
"Coinbase"    → https://api.coinbase.com
"KuCoin"      → https://api.kucoin.com
"Bybit"       → https://api.bybit.com
"OKX"         → https://www.okx.com
"PancakeSwap" → https://api.dexscreener.com
"Hyperliquid" → https://api.hyperliquid.xyz
```

### Service Lifetimes

| Lifetime | Services |
|----------|----------|
| **Singleton** | `JsonStorageService`, `ArbitrageDetector` |
| **Scoped** | All 7 exchange clients, `ExchangeService`, `AlertService` |
| **Hosted** | `PriceMonitorService` (long-running background task) |

### Middleware Pipeline

```
app.UseCors()               → Enables CORS for React frontend (localhost:3000)
app.UseHttpsRedirection()   → Redirects HTTP to HTTPS
app.UseAuthorization()      → Authorization middleware
app.MapControllers()        → Routes to API controllers
app.MapHub<AlertHub>(...)   → Maps SignalR hub at /hubs/alerts
```

---

## Models

### ExchangePrice
Represents a single price point from one exchange for one trading pair.

| Property | Type | Description |
|----------|------|-------------|
| `Exchange` | string | Exchange name (e.g. "Binance", "OKX") |
| `Symbol` | string | Trading pair (e.g. "BTC/USDT") |
| `Price` | decimal | Current price in quote currency |
| `Volume24h` | decimal | 24-hour trading volume |
| `Timestamp` | DateTime | UTC timestamp when price was fetched |

### PriceComparison
Aggregates prices from all exchanges for a single trading pair and calculates the spread.

| Property | Type | Description |
|----------|------|-------------|
| `Symbol` | string | Trading pair |
| `Prices` | List\<ExchangePrice\> | All exchange prices for this pair |
| `Spread` | decimal | Absolute price difference (highest - lowest) |
| `SpreadPercent` | decimal | Spread as percentage of the lowest price |
| `HighestExchange` | string | Exchange with the highest price |
| `LowestExchange` | string | Exchange with the lowest price |

### AlertLog
Represents a triggered arbitrage alert, persisted to `alerts-log.json`.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Unique GUID for each alert |
| `Symbol` | string | Trading pair that triggered the alert |
| `HighExchange` | string | Exchange where price is highest (sell here) |
| `LowExchange` | string | Exchange where price is lowest (buy here) |
| `HighPrice` | decimal | The highest price across exchanges |
| `LowPrice` | decimal | The lowest price across exchanges |
| `SpreadPercent` | decimal | The spread percentage at trigger time |
| `TriggeredAt` | DateTime | UTC timestamp when alert was triggered |

---

## DTOs & Metadata (Buddy Class Pattern)

DTOs mirror their corresponding Models but use nullable string properties (`string?`) to decouple the API contract from the internal domain. Both the Model and its DTO reference the **same Metadata class** via `[ModelMetadataType]`:

```csharp
// Model
[ModelMetadataType(typeof(ExchangePriceMetadata))]
public class ExchangePrice { ... }

// DTO
[ModelMetadataType(typeof(ExchangePriceMetadata))]
public class ExchangePriceDTO { ... }

// Shared validation rules
public class ExchangePriceMetadata
{
    [Required] [StringLength(50)] public string Exchange { get; set; }
    [Required] [StringLength(20)] public string Symbol { get; set; }
    [Required] [Range(0, max)]    public decimal Price { get; set; }
    ...
}
```

Mapping is done **manually in controllers** (Model → DTO) using LINQ `.Select()` projections — no AutoMapper dependency needed.

---

## ExchangeService — Core Orchestrator

`ExchangeService` is the central aggregation service. It fans out price requests to all 7 exchanges in parallel using `Task.WhenAll`, then calculates the spread.

### GetComparisonAsync(symbol)

1. Creates 7 async tasks — one per exchange client
2. Awaits all in parallel (`Task.WhenAll`)
3. Filters out `null` results (exchange didn't have that pair)
4. Requires at least **2 successful prices** for a valid comparison
5. Finds the highest and lowest price across exchanges
6. Calculates:
   - `Spread = HighestPrice - LowestPrice`
   - `SpreadPercent = (Spread / LowestPrice) * 100`
7. Returns a `PriceComparison` object

### GetAllComparisonsAsync(symbols)

Runs `GetComparisonAsync` for all 21 trading pairs in parallel. Returns only pairs that had sufficient data.

---

## ArbitrageDetector — 3% Threshold Rule

```csharp
private const decimal SpreadThresholdPercent = 3.0m;
```

Iterates through all `PriceComparison` objects. If `SpreadPercent >= 3.0%`, creates an `AlertLog` capturing:
- Which pair triggered the alert
- Where to **buy** (lowest exchange + price)
- Where to **sell** (highest exchange + price)
- The spread percentage at that moment
- UTC timestamp

**Why 3%?** Typical CEX-to-CEX spreads are < 0.1% for major pairs. CEX-to-DEX spreads can be 0.5–2% due to gas fees and slippage. A 3%+ spread strongly suggests a real arbitrage opportunity after accounting for trading fees and transfer costs.

---

## PriceMonitorService — Background Worker

`PriceMonitorService` extends `BackgroundService`. It runs continuously in the background, independent of HTTP requests.

### Execution Loop (every 30 seconds)

```
1. Create a DI scope (BackgroundService is singleton, exchange clients are scoped)
2. Fetch prices from all 7 exchanges for all 21 pairs
3. Save every price snapshot to price-history.json (append)
4. Run ArbitrageDetector.Check() on all comparisons
5. For each triggered alert (spread > 3%):
   a) Log to console via ILogger
   b) Append to alerts-log.json
   c) Push to all connected WebSocket clients via SignalR
6. Wait 30 seconds, repeat
```

Each cycle is wrapped in `try/catch` — a failure does not crash the service. It logs the error and continues on the next cycle.

---

## JsonStorageService — Thread-Safe File Persistence

Provides simple file-based persistence using JSON arrays stored in the `/Data` directory.

| File | Contents |
|------|----------|
| `Data/price-history.json` | Array of `PriceComparison` snapshots |
| `Data/alerts-log.json` | Array of `AlertLog` entries |

### Thread Safety

Uses `SemaphoreSlim(1, 1)` to serialize all file access. This prevents race conditions when the background service and HTTP requests try to read/write simultaneously.

### Methods

| Method | Description |
|--------|-------------|
| `ReadAsync<T>(fileName)` | Deserialize JSON array from file |
| `WriteAsync<T>(fileName, data)` | Serialize & overwrite entire file |
| `AppendAsync<T>(fileName, item)` | Read → Add → Write (atomic append) |

Auto-creates the `/Data` directory on startup. Returns an empty list if the file doesn't exist or if the JSON is corrupted.

---

## AlertService — Logging + Real-Time Push

| Method | Description |
|--------|-------------|
| `LogAndBroadcastAsync(alert)` | Persist alert to `alerts-log.json` + push via SignalR `"AlertTriggered"` event |
| `GetHistoryAsync()` | Read all alerts from `alerts-log.json` |

---

## SignalR Hub — Real-Time WebSocket

`AlertHub` is mapped at `/hubs/alerts`. It enables real-time push notifications to connected clients (e.g. React frontend).

**Event:** `"AlertTriggered"`
**Payload:** `AlertLog` object (symbol, exchanges, prices, spread, timestamp)

```
Frontend connects → WebSocket at ws://localhost:5000/hubs/alerts
Backend detects arbitrage → AlertService pushes "AlertTriggered"
Frontend receives → Updates UI in real-time (no polling needed)
```

CORS is configured to allow credentials from `http://localhost:3000` (React dev server) — required for SignalR WebSocket handshake.

---

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/prices` | GET | Live prices for all 21 pairs from 7 exchanges |
| `/api/prices/{symbol}` | GET | Live prices for one specific pair (e.g. `/api/prices/BTC%2FUSDT`) |
| `/api/alerts/history` | GET | All previously triggered arbitrage alerts (newest first) |
| `/hubs/alerts` | WebSocket | Real-time arbitrage alert push via SignalR |
| `/swagger` | GET | API documentation (development only) |

---

## Data Flow Diagram

```
                        EVERY 30 SECONDS
                              │
              PriceMonitorService (BackgroundService)
                              │
                              ▼
          ExchangeService.GetAllComparisonsAsync(21 pairs)
                              │
          ┌───────────────────┼───────────────────┐
          │                   │                   │
    ┌─────┴─────┐     ┌──────┴──────┐     ┌──────┴──────┐
    │  Binance   │     │   Bybit     │     │ PancakeSwap │
    │  Coinbase  │     │   OKX       │     │ Hyperliquid │
    │  KuCoin    │     │             │     │             │
    └─────┬─────┘     └──────┬──────┘     └──────┬──────┘
          │ 5 CEX APIs       │                   │ 2 DEX APIs
          └───────────────────┼───────────────────┘
                              │ Task.WhenAll (parallel)
                              ▼
                 List<PriceComparison> (spread calculated)
                              │
              ┌───────────────┼───────────────┐
              │                               │
              ▼                               ▼
    price-history.json              ArbitrageDetector.Check()
       (append)                     spread >= 3% ?
                                          │
                              ┌───── YES ──┴── NO ─────┐
                              │                        │
                              ▼                     (skip)
                    AlertService.LogAndBroadcast()
                              │
                    ┌─────────┼─────────┐
                    │                   │
                    ▼                   ▼
          alerts-log.json        SignalR Push
             (append)          "AlertTriggered"
                                    │
                                    ▼
                            Connected Clients
                          (React frontend / WS)
```

```
                     ON-DEMAND (HTTP Request)

    Client (React / Swagger / curl)
              │
              ├── GET /api/prices ──────────► Live prices for all 21 pairs
              ├── GET /api/prices/{symbol} ─► Live prices for one pair
              ├── GET /api/alerts/history ──► All past triggered alerts
              └── WS  /hubs/alerts ─────────► Real-time alert push
```

---

## Key Design Decisions

1. **Parallel API Calls (`Task.WhenAll`)** — All 7 exchanges are queried simultaneously. Total time = slowest single exchange, not the sum of all 7.

2. **Graceful Degradation** — If an exchange is down or doesn't list a token, its client returns `null`. The system continues with however many exchanges respond (minimum 2).

3. **Named HttpClients (`IHttpClientFactory`)** — Each exchange gets a pre-configured `HttpClient`. Avoids socket exhaustion and cleanly separates exchange configuration.

4. **Background Service + Manual Scoping** — `PriceMonitorService` is a singleton but exchange clients are scoped. Each 30-second cycle manually creates a DI scope via `IServiceProvider.CreateScope()`.

5. **Thread-Safe Storage (`SemaphoreSlim`)** — Prevents data corruption when the background service writes while an HTTP request reads simultaneously.

6. **Hardcoded 3% Threshold** — Simplifies the system. Accounts for typical trading fees (~0.1% per trade x2) plus blockchain transfer fees, providing a realistic actionable signal.

7. **Model-DTO Separation with Shared Metadata** — Models hold internal state; DTOs shape the API response. Both reference the same Metadata class, ensuring validation consistency without duplication.

---

## How to Run

**Prerequisites:** .NET 9 SDK

```bash
# 1. Navigate to project directory
cd CEX-DEX-Parser/CEX-DEX-Parser

# 2. Run the backend
dotnet run

# 3. Open another terminal for the React frontend
cd CEX-DEX-Parser/cex-dex-client
npm install    # first run only
npm run dev

# 4. Access Swagger UI at:
#    https://localhost:5001/swagger
```

The `PriceMonitorService` starts automatically and begins fetching prices every 30 seconds from all 7 exchanges. Arbitrage alerts with spread > 3% are saved to `Data/alerts-log.json`, pushed via WebSocket to `/hubs/alerts`, and logged to console output.
