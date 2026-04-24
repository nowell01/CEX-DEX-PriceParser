# CEX-DEX-PriceParser

To run the project properly:
 1. Open Terminal
 2. cd CEX-DEX-Parser
 3. dotnet run
 4. Open another Terminal
 5. cd CEX-DEX-Parser/cex-dex-client
 6. npm install (if first run)
 7. npm run dev

// ============================================================================
//  CryptoScan (CEX-DEX-Parser) — Full Code Overview
// ============================================================================
//
//  A real-time cryptocurrency arbitrage detection system built with
//  ASP.NET Core (.NET 9) Web API. The application compares live prices
//  across 7 exchanges (5 CEX + 2 DEX), detects arbitrage opportunities
//  when the price spread exceeds 3%, and pushes real-time alerts to
//  connected clients via SignalR WebSocket.
//
// ============================================================================


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  1. PROJECT ARCHITECTURE                                                 ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  Technology Stack:
//    - .NET 9, ASP.NET Core Web API
//    - SignalR (WebSocket real-time communication)
//    - IHttpClientFactory (named HTTP clients for each exchange)
//    - JSON file storage with thread-safe SemaphoreSlim locking
//    - Swagger/OpenAPI for API documentation
//
//  Architectural Pattern:
//    - MVC-style separation using Models, DTOs, Metadata, Controllers, Services
//    - Models = internal domain objects
//    - DTOs   = data transfer objects exposed through API (hides internals)
//    - Metadata = DataAnnotation validation rules shared between Model and DTO
//      via [ModelMetadataType] attribute (buddy class pattern)
//
//  Folder Structure:
//    CEX-DEX-Parser/
//    ├── Controllers/          API endpoints (PricesController, AlertsController)
//    ├── DTOs/                 Data Transfer Objects for API responses
//    ├── Hubs/                 SignalR hub for real-time alert push
//    ├── Metadata/             Validation attributes shared by Models & DTOs
//    ├── Models/               Internal domain models
//    ├── Services/             Business logic, exchange clients, background jobs
//    ├── Data/                 JSON storage files (auto-created at runtime)
//    ├── Program.cs            Application entry point & DI configuration
//    └── appsettings.json      Exchange API base URLs
//
//  Exchanges Integrated (7 total):
//    CEX (Centralized):        DEX (Decentralized):
//    ├── Binance               ├── PancakeSwap (via DexScreener API)
//    ├── Coinbase              └── Hyperliquid
//    ├── KuCoin
//    ├── Bybit
//    └── OKX
//
//  Trading Pairs Tracked (21 total):
//    BTC/USDT, ETH/USDT, SOL/USDT, XRP/USDT, SUI/USDT, DOGE/USDT,
//    ADA/USDT, LTC/USDT, AVAX/USDT, TON/USDT, AAVE/USDT, APEX/USDT,
//    ARB/USDT, BNB/USDT, STRK/USDT, TRX/USDT, PEPE/USDT, LINK/USDT,
//    TRUMP/USDT, SHIB/USDT, XLM/USDT


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  2. PROGRAM.CS — Entry Point & Dependency Injection                      ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  Program.cs is the application's composition root. It configures all
//  services using the built-in ASP.NET Core dependency injection container.
//
//  Key registrations:
//
//  ┌──────────────────────────────────────────────────────────────────┐
//  │  Named HttpClients — one per exchange with base URL + timeout    │
//  │                                                                  │
//  │  "Binance"     → https://api.binance.com                         │
//  │  "Coinbase"    → https://api.coinbase.com                        │
//  │  "KuCoin"      → https://api.kucoin.com                          │
//  │  "Bybit"       → https://api.bybit.com                           │
//  │  "OKX"         → https://www.okx.com                             │
//  │  "PancakeSwap" → https://api.dexscreener.com                     │
//  │  "Hyperliquid" → https://api.hyperliquid.xyz                     │
//  │                                                                  │
//  │  Each client has a 10-second timeout to prevent hangs.           │
//  └──────────────────────────────────────────────────────────────────┘
//
//  Service Lifetimes:
//    Singleton  → JsonStorageService (one file writer for entire app)
//    Singleton  → ArbitrageDetector  (stateless, thread-safe detector)
//    Scoped     → All exchange clients (one per HTTP request scope)
//    Scoped     → ExchangeService, AlertService
//    Hosted     → PriceMonitorService (long-running background task)
//
//  Middleware Pipeline (order matters):
//    app.UseCors()                → Enables CORS for React frontend
//    app.UseHttpsRedirection()    → Redirects HTTP to HTTPS
//    app.UseAuthorization()       → Authorization middleware
//    app.MapControllers()         → Routes to API controllers
//    app.MapHub<AlertHub>(...)    → Maps SignalR hub at /hubs/alerts


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  3. MODELS — Domain Objects                                              ║
// ╚══════════════════════════════════════════════════════════════════════════╝

// --- ExchangePrice.cs ---
// Represents a single price point from one exchange for one trading pair.
// [ModelMetadataType(typeof(ExchangePriceMetadata))] links validation rules.
//
// Properties:
//   Exchange   (string)  — Name of the exchange (e.g. "Binance", "OKX")
//   Symbol     (string)  — Trading pair (e.g. "BTC/USDT")
//   Price      (decimal) — Current price in quote currency
//   Volume24h  (decimal) — 24-hour trading volume
//   Timestamp  (DateTime)— UTC timestamp when price was fetched

// --- PriceComparison.cs ---
// Aggregates prices from all exchanges for a single trading pair and
// calculates the spread (difference between highest and lowest price).
//
// Properties:
//   Symbol          (string)             — Trading pair
//   Prices          (List<ExchangePrice>)— All exchange prices for this pair
//   Spread          (decimal)            — Absolute price difference (high - low)
//   SpreadPercent   (decimal)            — Spread as percentage of lowest price
//   HighestExchange (string)             — Exchange with the highest price
//   LowestExchange  (string)             — Exchange with the lowest price

// --- AlertLog.cs ---
// Represents a triggered arbitrage alert, persisted to alerts-log.json.
//
// Properties:
//   Id             (string)  — Unique GUID for each alert
//   Symbol         (string)  — Trading pair that triggered the alert
//   HighExchange   (string)  — Exchange where price is highest (sell here)
//   LowExchange    (string)  — Exchange where price is lowest  (buy here)
//   HighPrice      (decimal) — The highest price across exchanges
//   LowPrice       (decimal) — The lowest price across exchanges
//   SpreadPercent  (decimal) — The spread percentage at trigger time
//   TriggeredAt    (DateTime)— UTC timestamp when alert was triggered


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  4. DTOs — Data Transfer Objects (API Response Layer)                    ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  DTOs mirror their corresponding Models but use nullable string
//  properties (string?) to decouple the API contract from the internal
//  domain. This follows the MVC "buddy class" pattern where both the
//  Model and DTO reference the same Metadata class for validation.
//
//  ExchangePriceDTO    → sent inside PriceComparisonDTO.Prices list
//  PriceComparisonDTO  → returned by GET /api/prices endpoints
//  AlertLogDTO         → returned by GET /api/alerts/history
//
//  Mapping is done manually in controllers (Model → DTO) using LINQ
//  .Select() projections. This keeps the architecture simple without
//  requiring AutoMapper or similar libraries.


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  5. METADATA — Shared Validation Rules                                   ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  Each Metadata class contains DataAnnotation attributes that define
//  validation rules. Both the Model and its DTO reference the same
//  Metadata class via [ModelMetadataType(typeof(...))].
//
//  ExchangePriceMetadata:
//    [Required] Exchange, Symbol, Timestamp
//    [StringLength(50)] Exchange | [StringLength(20)] Symbol
//    [Range(0, max)] Price, Volume24h
//
//  PriceComparisonMetadata:
//    [Required] Symbol
//    [Range(0, max)] Spread | [Range(0, 100)] SpreadPercent
//    [StringLength(50)] HighestExchange, LowestExchange
//
//  AlertLogMetadata:
//    [Required] Id, Symbol, HighExchange, LowExchange, TriggeredAt
//    [Range(0, max)] HighPrice, LowPrice | [Range(0, 100)] SpreadPercent


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  6. EXCHANGE CLIENTS — Individual API Integrations                       ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  All exchange clients follow the same contract:
//    public async Task<ExchangePrice?> GetPriceAsync(string symbol)
//
//  Each client:
//    1. Receives a named HttpClient via IHttpClientFactory
//    2. Converts the universal symbol format ("BTC/USDT") to the
//       exchange-specific format
//    3. Calls the exchange's public API (no authentication needed for
//       public market data, except where noted)
//    4. Parses the JSON response and returns an ExchangePrice object
//    5. Returns null on failure (logged as warning, never crashes)

// ─────────────────────────────────────────────────────────────────────
// BinanceClient.cs — Centralized Exchange
// ─────────────────────────────────────────────────────────────────────
//  API:         GET /api/v3/ticker/24hr?symbol={BTCUSDT}
//  Symbol fmt:  Concatenated — "BTC/USDT" → "BTCUSDT"
//  Response:    { "lastPrice": "67000.00", "quoteVolume": "123456.78" }
//  Returns:     Price + 24h volume in quote currency

// ─────────────────────────────────────────────────────────────────────
// CoinbaseClient.cs — Centralized Exchange
// ─────────────────────────────────────────────────────────────────────
//  API:         GET /v2/prices/{BTC-USD}/spot
//  Symbol fmt:  Dash-separated, USDT→USD — "BTC/USDT" → "BTC-USD"
//  Response:    { "data": { "amount": "67000.00" } }
//  Note:        Coinbase spot endpoint does not return 24h volume.
//               Maps USDT pairs to USD (Coinbase doesn't support USDT).

// ─────────────────────────────────────────────────────────────────────
// KuCoinClient.cs — Centralized Exchange
// ─────────────────────────────────────────────────────────────────────
//  API:         GET /api/v1/market/orderbook/level1?symbol={BTC-USDT}
//  Symbol fmt:  Dash-separated — "BTC/USDT" → "BTC-USDT"
//  Response:    { "data": { "price": "67000.00" } }
//  Note:        Uses order book level 1 (best bid/ask midpoint).
//               Checks for null data (token not listed on KuCoin).

// ─────────────────────────────────────────────────────────────────────
// BybitClient.cs — Centralized Exchange
// ─────────────────────────────────────────────────────────────────────
//  API:         GET /v5/market/tickers?category=spot&symbol={BTCUSDT}
//  Symbol fmt:  Concatenated — "BTC/USDT" → "BTCUSDT"
//  Response:    { "result": { "list": [{ "lastPrice": "67000",
//                                        "volume24h": "1234" }] } }
//  Note:        Uses Bybit V5 unified API. Returns price + volume.

// ─────────────────────────────────────────────────────────────────────
// OkxClient.cs — Centralized Exchange
// ─────────────────────────────────────────────────────────────────────
//  API:         GET /api/v5/market/ticker?instId={BTC-USDT}
//  Symbol fmt:  Dash-separated — "BTC/USDT" → "BTC-USDT"
//  Response:    { "data": [{ "last": "67000",
//                            "volCcy24h": "82345678" }] }
//  Note:        volCcy24h = 24h volume in quote currency (USDT).

// ─────────────────────────────────────────────────────────────────────
// PancakeSwapClient.cs — Decentralized Exchange (BSC)
// ─────────────────────────────────────────────────────────────────────
//  API:         DexScreener — GET /latest/dex/tokens/{bsc_address}
//  Approach:    Uses a hardcoded dictionary of BEP-20 token contract
//               addresses on Binance Smart Chain (BSC).
//  Flow:
//    1. Look up BEP-20 address for the base asset (e.g. BTC → 0x7130...)
//    2. Query DexScreener for all trading pairs involving that token
//    3. Filter results to: dexId starts with "pancakeswap" AND chainId == "bsc"
//    4. Pick the pair with the highest 24h volume
//    5. Extract priceUsd from the best pair
//  Tokens mapped: BTC, ETH, BNB, SOL, ADA, DOT, XRP, DOGE, LTC,
//                 AVAX, TON, AAVE, ARB (13 BSC token addresses)
//  Note:        Tokens not on BSC (SUI, APEX, etc.) return null gracefully.

// ─────────────────────────────────────────────────────────────────────
// HyperliquidClient.cs — Decentralized Exchange (Perpetuals)
// ─────────────────────────────────────────────────────────────────────
//  API:         POST /info  with body {"type":"allMids"}
//  Approach:    Returns a flat object with all listed assets and their
//               mid-market prices. No token address mapping needed —
//               uses the base asset name directly (e.g. "BTC").
//  Response:    { "BTC": "67000.5", "ETH": "3500.2", ... }
//  Note:        Perpetual futures prices, not spot. Volume not available
//               from this endpoint.


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  7. EXCHANGE SERVICE — Aggregation & Spread Calculation                  ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  ExchangeService is the core orchestrator. It fans out price requests
//  to all 7 exchanges in parallel using Task.WhenAll, then aggregates
//  the results.
//
//  GetComparisonAsync(symbol):
//    1. Creates 7 tasks — one per exchange client
//    2. Awaits all in parallel (Task.WhenAll)
//    3. Filters out null results (exchange didn't have that pair)
//    4. Requires at least 2 successful prices to form a comparison
//    5. Finds the highest and lowest price across all exchanges
//    6. Calculates:
//         Spread        = HighestPrice - LowestPrice
//         SpreadPercent = (Spread / LowestPrice) * 100
//    7. Returns a PriceComparison object
//
//  GetAllComparisonsAsync(symbols):
//    - Runs GetComparisonAsync for ALL 21 trading pairs in parallel
//    - Returns only pairs that had enough data (filters null results)
//
//  Example: BTC/USDT
//    Binance:     $67,000  ─┐
//    Coinbase:    $67,050   │
//    KuCoin:      $66,990   ├─→ Highest: Coinbase ($67,050)
//    Bybit:       $67,010   │   Lowest:  KuCoin   ($66,990)
//    OKX:         $67,020   │   Spread:  $60
//    PancakeSwap: $67,100   │   SpreadPercent: 0.0896%
//    Hyperliquid: $67,005  ─┘


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  8. ARBITRAGE DETECTOR — 3% Threshold Rule                               ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  ArbitrageDetector.Check(comparisons):
//    - Iterates through all PriceComparison objects
//    - If SpreadPercent >= 3.0% → creates an AlertLog entry
//    - The AlertLog captures:
//        • Which pair triggered (Symbol)
//        • Where to buy (LowExchange + LowPrice)
//        • Where to sell (HighExchange + HighPrice)
//        • The spread percentage at that moment
//        • UTC timestamp
//    - Returns a list of all triggered alerts for this cycle
//
//  Why 3%?
//    - Typical CEX-to-CEX spreads are < 0.1% for major pairs
//    - CEX-to-DEX spreads can be 0.5-2% due to gas fees & slippage
//    - A 3%+ spread strongly suggests a real arbitrage opportunity
//      after accounting for trading fees and transfer costs


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  9. PRICE MONITOR SERVICE — Background Worker                            ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  PriceMonitorService extends BackgroundService — it runs continuously
//  in the background as a hosted service, independent of HTTP requests.
//
//  Execution Loop (every 30 seconds):
//    ┌─────────────────────────────────────────────────────────────┐
//    │  1. Create a DI scope (BackgroundService is singleton,      │
//    │     but exchange clients are scoped — must resolve manually)│
//    │                                                             │
//    │  2. Fetch prices from all 7 exchanges for all 21 pairs      │
//    │     (via ExchangeService.GetAllComparisonsAsync)            │
//    │                                                             │
//    │  3. Save every price snapshot to price-history.json         │
//    │     (append each PriceComparison to the JSON array)         │
//    │                                                             │
//    │  4. Run ArbitrageDetector.Check() on all comparisons        │
//    │                                                             │
//    │  5. For each triggered alert (spread > 3%):                 │
//    │     a) Log to console via ILogger                           │
//    │     b) Append to alerts-log.json via JsonStorageService     │
//    │     c) Push to all connected WebSocket clients via SignalR  │
//    │                                                             │
//    │  6. Wait 30 seconds, repeat                                 │
//    └─────────────────────────────────────────────────────────────┘
//
//  Error Handling:
//    - Each cycle is wrapped in try/catch — a single failure does
//      not crash the background service. It logs the error and
//      continues on the next cycle.


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  10. JSON STORAGE SERVICE — Thread-Safe File Persistence                 ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  JsonStorageService provides simple file-based persistence using
//  JSON arrays stored in the /Data directory.
//
//  Files:
//    Data/price-history.json — Array of PriceComparison snapshots
//    Data/alerts-log.json    — Array of AlertLog entries
//
//  Thread Safety:
//    Uses SemaphoreSlim(1,1) to serialize all file access.
//    This prevents race conditions when the background service
//    and HTTP requests try to read/write simultaneously.
//
//  Methods:
//    ReadAsync<T>(fileName)       — Deserialize JSON array from file
//    WriteAsync<T>(fileName,data) — Serialize & overwrite entire file
//    AppendAsync<T>(fileName,item)— Read → Add → Write (atomic append)
//
//  Resilience:
//    - Auto-creates /Data directory on startup
//    - Returns empty list if file doesn't exist
//    - Returns empty list if JSON is corrupted (catches JsonException)


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  11. ALERT SERVICE — Logging + Real-Time Push                            ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  AlertService has two responsibilities:
//
//    LogAndBroadcastAsync(AlertLog alert):
//      1. Persist the alert to alerts-log.json (via JsonStorageService)
//      2. Push to all connected clients via SignalR:
//         hub.Clients.All.SendAsync("AlertTriggered", alert)
//
//    GetHistoryAsync():
//      - Reads all alerts from alerts-log.json
//      - Returns List<AlertLog> (sorted in controller)


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  12. SIGNALR HUB — Real-Time WebSocket Communication                     ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  AlertHub (mapped at /hubs/alerts) is a SignalR hub that enables
//  real-time push notifications to connected clients (e.g. React frontend).
//
//  Event: "AlertTriggered"
//    Payload: AlertLog object (symbol, exchanges, prices, spread, timestamp)
//
//  Flow:
//    Frontend connects → WebSocket established at ws://localhost:5000/hubs/alerts
//    Backend detects arbitrage → AlertService pushes "AlertTriggered" event
//    Frontend receives → Updates UI in real-time (no polling needed)
//
//  CORS is configured to allow credentials from http://localhost:3000
//  (React dev server) — required for SignalR WebSocket handshake.


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  13. API CONTROLLERS — HTTP Endpoints                                    ║
// ╚══════════════════════════════════════════════════════════════════════════╝

// --- PricesController.cs ---
//
//  [Route("api/prices")]
//
//  GET /api/prices
//    → Fetches live prices for all 21 default pairs from all 7 exchanges
//    → Returns List<PriceComparisonDTO> with spread data
//    → 404 if no data available
//
//  GET /api/prices/{symbol}
//    → Fetches live prices for a specific pair (e.g. /api/prices/BTC%2FUSDT)
//    → URL-decodes the symbol (%2F → /)
//    → Returns single PriceComparisonDTO
//    → 404 if pair not found on enough exchanges
//
//  Manual DTO mapping in controller (Model → DTO):
//    comparison.Prices.Select(p => new ExchangePriceDTO { ... })

// --- AlertsController.cs ---
//
//  [Route("api/alerts")]
//
//  GET /api/alerts/history
//    → Returns all previously triggered arbitrage alerts
//    → Sorted by TriggeredAt descending (newest first)
//    → Returns List<AlertLogDTO>
//    → 404 if no alerts have been triggered yet


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  14. DATA FLOW DIAGRAM                                                   ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  ┌─────────────────────────────────────────────────────────────────────┐
//  │                     EVERY 30 SECONDS                                │
//  │                                                                     │
//  │  PriceMonitorService (BackgroundService)                            │
//  │       │                                                             │
//  │       ▼                                                             │
//  │  ExchangeService.GetAllComparisonsAsync(21 pairs)                   │
//  │       │                                                             │
//  │       ├── BinanceClient ──────► Binance REST API                    │
//  │       ├── CoinbaseClient ─────► Coinbase REST API                   │
//  │       ├── KuCoinClient ───────► KuCoin REST API                     │
//  │       ├── BybitClient ────────► Bybit V5 REST API                   │
//  │       ├── OkxClient ──────────► OKX REST API          ┌───────────┐ │
//  │       ├── PancakeSwapClient ──► DexScreener API       │ 7 API     │ │
//  │       └── HyperliquidClient ──► Hyperliquid POST API  │ calls per │ │
//  │                                                       │ pair      │ │
//  │       │ Task.WhenAll (parallel)                       └───────────┘ │
//  │       ▼                                                             │
//  │  List<PriceComparison> (spread calculated per pair)                 │
//  │       │                                                             │
//  │       ├──────────────────────► price-history.json (append)          │
//  │       │                                                             │
//  │       ▼                                                             │
//  │  ArbitrageDetector.Check() ── spread >= 3%?                         │
//  │       │                                                             │
//  │       ├── YES ──► AlertService.LogAndBroadcastAsync()               │
//  │       │                ├──► alerts-log.json (append)                │
//  │       │                └──► SignalR → "AlertTriggered" → Frontend   │
//  │       │                                                             │
//  │       └── NO ───► Skip (no alert triggered)                         │
//  └─────────────────────────────────────────────────────────────────────┘
//
//  ┌─────────────────────────────────────────────────────────────────────┐
//  │                     ON-DEMAND (HTTP Request)                        │
//  │                                                                     │
//  │  Client (React / Swagger / curl)                                    │
//  │       │                                                             │
//  │       ├── GET /api/prices ──────► Live prices for all 21 pairs      │
//  │       ├── GET /api/prices/{sym} ► Live prices for one specific pair │
//  │       ├── GET /api/alerts/history ► All past triggered alerts       │
//  │       └── WS  /hubs/alerts ─────► Real-time alert push (SignalR)    │
//  └─────────────────────────────────────────────────────────────────────┘


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  15. KEY DESIGN DECISIONS                                                ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  1. Parallel API Calls (Task.WhenAll)
//     All 7 exchanges are queried simultaneously per trading pair.
//     This minimizes latency — total time = slowest single exchange,
//     not the sum of all 7.
//
//  2. Graceful Degradation
//     If an exchange is down or doesn't list a token, its client
//     returns null. The system still works with however many
//     exchanges respond (minimum 2 for a valid comparison).
//
//  3. Named HttpClients (IHttpClientFactory)
//     Each exchange gets its own named HttpClient with a pre-configured
//     base URL and timeout. This avoids socket exhaustion and provides
//     clean separation of exchange-specific configuration.
//
//  4. Background Service + Scoped Dependency Resolution
//     PriceMonitorService is a singleton (BackgroundService), but the
//     exchange clients are scoped. We manually create a DI scope
//     (IServiceProvider.CreateScope()) each cycle to resolve them.
//
//  5. Thread-Safe JSON Storage (SemaphoreSlim)
//     JsonStorageService uses SemaphoreSlim(1,1) to serialize file
//     access. This prevents data corruption when the background
//     service writes while an HTTP request reads simultaneously.
//
//  6. Hardcoded 3% Threshold
//     Simplifies the system — no user configuration needed.
//     3% accounts for typical trading fees (~0.1% per trade x2)
//     plus blockchain transfer fees, providing a realistic
//     actionable arbitrage signal.
//
//  7. Model-DTO Separation with Shared Metadata
//     Models hold internal state; DTOs shape the API response.
//     Both reference the same Metadata class for validation,
//     ensuring consistency without code duplication.


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  16. API ENDPOINTS SUMMARY                                               ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  ┌─────────────────────────┬────────┬────────────────────────────────┐
//  │ Endpoint                │ Method │ Description                    │
//  ├─────────────────────────┼────────┼────────────────────────────────┤
//  │ /api/prices             │ GET    │ All 21 pairs, live prices      │
//  │ /api/prices/{symbol}    │ GET    │ Single pair, live prices       │
//  │ /api/alerts/history     │ GET    │ All triggered alert logs       │
//  │ /hubs/alerts            │ WS     │ Real-time alert push (SignalR) │
//  │ /swagger                │ GET    │ API documentation (dev only)   │
//  └─────────────────────────┴────────┴────────────────────────────────┘


// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  17. HOW TO RUN                                                          ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
//  Prerequisites: .NET 9 SDK
//
//  1. Navigate to project directory:
//     cd CEX-DEX-Parser/CEX-DEX-Parser
//
//  2. Run the application:
//     dotnet run
//
//  3. Access Swagger UI:
//     https://localhost:5001/swagger
//
//  4. The PriceMonitorService starts automatically and begins
//     fetching prices every 30 seconds from all 7 exchanges.
//
//  5. Arbitrage alerts with spread > 3% are:
//     - Saved to Data/alerts-log.json
//     - Pushed via WebSocket to /hubs/alerts
//     - Logged to console output
