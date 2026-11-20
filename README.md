# CEX·DEX Arbitrage Dashboard

A full-stack, real-time arbitrage monitoring system that tracks price spreads across **5 centralized** and **2 decentralized exchanges**, detects arbitrage opportunities, stores price and alert history in **PostgreSQL**, generates **AI-written alert summaries**, and delivers live alerts through a React dashboard and a Telegram bot.

**Live Demo:** https://cex-dex-client.vercel.app  
**Backend API:** https://cex-dex-parser-h7b3f0gwbyfah7ft.canadacentral-01.azurewebsites.net

---

## Overview

The system polls 21 trading pairs every 5 minutes across 7 exchanges, compares prices, and fires an alert whenever a spread exceeds 3%. Alerts are pushed to the React frontend in real time via SignalR and sent to all Telegram subscribers. Every scan and alert is persisted to PostgreSQL, and an LLM (via Groq) turns raw alert data into short, plain-language insights.

```
Exchanges ─► PriceMonitorService (every 5 min)
                    │
                    ├─► IPriceHistoryStore ─► PostgreSQL (price_snapshots, exchange_prices)
                    ▼
            ArbitrageDetector (spread > 3%)
                    │
                    ▼
               AlertService
        ┌───────────┼──────────────┐
        ▼           ▼              ▼
   SignalR Hub  TelegramNotifier  IAlertLogStore ─► PostgreSQL (alert_logs)
   (dashboard)  (all subscribers)       │
                                        ▼
                              AlertInsightsService ─► Groq LLM ─► AI summary
```

---

## Tech Stack

### Backend — `CEX-DEX-Parser/`
| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9 / ASP.NET Core |
| Database | PostgreSQL 18 |
| ORM / Migrations | Entity Framework Core 9 + Npgsql, snake_case naming (`EFCore.NamingConventions`) |
| Real-time | SignalR (`AlertHub`) |
| Background work | `BackgroundService` (`PriceMonitorService`) |
| AI | Groq API (LLM alert summaries) via `AlertInsightsService` |
| Caching | `IMemoryCache` |
| Notifications | Telegram Bot API (`Telegram.Bot`) |
| Hosting | Azure App Service (planned move to Linux containers, see Roadmap) |

### Frontend — `cex-dex-client/`
| Layer | Technology |
|-------|-----------|
| Framework | React + Vite |
| Real-time | `@microsoft/signalr` |
| Styling | CSS / Tailwind |
| Hosting | Vercel |

---

## Exchanges

| Exchange | Type | Data Source |
|----------|------|-------------|
| Binance | CEX | REST API |
| Coinbase | CEX | REST API |
| KuCoin | CEX | REST API |
| Bybit | CEX | REST API |
| OKX | CEX | REST API |
| PancakeSwap | DEX (BSC) | DexScreener API |
| Hyperliquid | DEX (Perp) | REST API |

---

## Monitored Trading Pairs

21 pairs tracked across all exchanges:

`BTC/USDT` `ETH/USDT` `SOL/USDT` `XRP/USDT` `SUI/USDT` `DOGE/USDT` `ADA/USDT` `LTC/USDT` `AVAX/USDT` `GRAM/USDT` `AAVE/USDT` `APEX/USDT` `ARB/USDT` `BNB/USDT` `STRK/USDT` `TRX/USDT` `PEPE/USDT` `LINK/USDT` `TRUMP/USDT` `SHIB/USDT` `XLM/USDT`

---

## Key Features

### Real-Time Price Monitoring
- Polls all 7 exchanges every **5 minutes** via `PriceMonitorService`
- Persists every scan to PostgreSQL (one `price_snapshots` row per symbol, one `exchange_prices` row per exchange)
- Handles per-exchange failures gracefully — one exchange going down doesn't break the cycle

### Arbitrage Detection
- `ArbitrageDetector` compares prices across all exchange pairs for each symbol
- Fires an alert when spread exceeds **3%**
- Calculates low/high exchange, low/high price, and spread %

### AI Alert Summaries (Groq)
- `AlertInsightsService` sends **aggregated** recent alert data (not raw rows) to a Groq-hosted LLM
- Returns a short, plain-language summary: which pairs and exchanges are producing spreads, how often, and notable patterns
- Results are cached with `IMemoryCache` to stay within Groq free-tier rate limits and keep responses fast
- Exposed via `GET /api/alerts/insights`
- Model: `openai/gpt-oss-120b`
- Summaries are informational only, not trading advice

### Live Dashboard (React)
- Connects via **SignalR WebSocket** for instant alert delivery
- Live price comparisons per symbol with highest/lowest exchange highlighted
- Real-time alerts panel and alert history panel
- Auto-refreshes price data every 30 seconds
- Fully responsive — mobile and desktop

### Telegram Bot Notifications
- Users subscribe by sending `/start` to the bot
- Chat IDs stored in PostgreSQL (`telegram_subscribers`) via the webhook at `/api/telegram/webhook`
- Duplicate `/start` messages are ignored atomically (`INSERT … ON CONFLICT DO NOTHING`)
- Every alert is broadcast to **all subscribers**
- Per-symbol **4-minute cooldown** prevents alert spam

### Alert History
- All fired alerts stored in PostgreSQL (`alert_logs`), indexed by symbol and time
- Accessible via `GET /api/alerts/history`

### PancakeSwap DEX Integration
- Uses the **DexScreener API** to fetch BSC token prices
- Maps 18 token symbols to verified BEP-20 contract addresses
- Selects the highest-volume PancakeSwap pair per token
- Skips tokens with no official BSC liquidity (SUI, APEX, STRK, TRUMP) instead of returning unreliable prices

---

## Data Layer

Storage moved from JSON flat files to **PostgreSQL with EF Core migrations**.

| Table | Contents |
|-------|----------|
| `price_snapshots` | One row per symbol per scan: spread, spread %, highest/lowest exchange, captured time |
| `exchange_prices` | Per-exchange price and 24h volume for each snapshot (FK → `price_snapshots`, cascade delete) |
| `alert_logs` | Fired arbitrage alerts |
| `telegram_subscribers` | Telegram chat IDs |
| `__EFMigrationsHistory` | Applied migrations (managed by EF Core) |

**Design notes**
- **Storage abstraction:** services depend on `IPriceHistoryStore`, `IAlertLogStore`, and `IChatIdStore` (`Services/Storage/`), not on the database directly — implementations can be swapped or faked in tests via DI.
- **Singleton-safe data access:** stores use `IDbContextFactory<AppDbContext>`, creating a short-lived context per operation, so they work inside singletons and the background service.
- **Exact prices:** decimals map to unbounded `numeric`, so micro-priced tokens (PEPE, SHIB) and BTC are stored without rounding.
- **UTC everywhere:** all timestamps are normalised to UTC (`timestamp with time zone`).
- **Migrations applied on startup** with `Database.MigrateAsync()` (single-replica deployment).
- **One-time JSON importer** (`Data/JsonImporter.cs`) migrated existing history.

---

## Project Structure

```
CEX-DEX-Parser/                       # .NET 9 backend
├── Controllers/
│   ├── AlertsController.cs           # GET /api/alerts/history, AI insights
│   ├── PricesController.cs           # GET /api/prices, /api/prices/{symbol}
│   └── TelegramWebhookController.cs  # POST /api/telegram/webhook
├── Data/
│   ├── AppDbContext.cs               # EF Core context + table configuration
│   ├── DesignTimeDbContextFactory.cs # Used by `dotnet ef`
│   ├── JsonImporter.cs               # One-time JSON → Postgres import
│   └── Entities/                     # PriceSnapshot, ExchangePriceRow, TelegramSubscriber
├── Migrations/                       # EF Core migrations
├── DTOs/
├── Hubs/
│   └── AlertHub.cs                   # SignalR hub
├── Metadata/
├── Models/
│   ├── AlertLog.cs
│   ├── ExchangePrice.cs
│   └── PriceComparison.cs
├── Services/
│   ├── Storage/                      # IChatIdStore, IAlertLogStore, IPriceHistoryStore + Postgres implementations
│   ├── AlertInsightsService.cs       # Groq LLM alert summaries
│   ├── AlertService.cs               # Log + broadcast + Telegram
│   ├── ArbitrageDetector.cs          # Spread detection logic
│   ├── ExchangeService.cs            # Aggregates all exchange clients
│   ├── PriceMonitorService.cs        # BackgroundService — main polling loop
│   ├── TelegramNotifier.cs           # Broadcast to all subscribers
│   └── *Client.cs                    # Binance, Bybit, Coinbase, KuCoin, OKX, PancakeSwap, Hyperliquid
└── Program.cs

cex-dex-client/                       # React + Vite frontend
├── src/
│   ├── api.js                        # REST fetch helpers
│   ├── signalr.js                    # SignalR connection factory
│   └── components/
│       ├── AlertsPanel.jsx
│       ├── PriceTable.jsx
│       └── ...
└── vite.config.js
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/prices` | Current prices across all exchanges |
| `GET` | `/api/prices/{symbol}` | Prices for a specific symbol |
| `GET` | `/api/alerts/history` | Historical arbitrage alerts |
| `GET` | `/api/alerts/insights` | AI-generated summary of recent alerts <!-- TODO: confirm route --> |
| `POST` | `/api/telegram/webhook` | Telegram bot webhook (receives `/start`) |
| `WS` | `/hubs/alerts` | SignalR hub — real-time alert stream |

---

## Setup & Configuration

### Prerequisites
- .NET 9 SDK
- PostgreSQL 18 (Homebrew, Postgres.app, or Docker)
- Node.js 18+
- EF Core CLI: `dotnet tool install --global dotnet-ef --version 9.*`
- A Telegram bot token ([@BotFather](https://t.me/BotFather))
- A Groq API key ([console.groq.com](https://console.groq.com))

### 1. Database

```sql
CREATE ROLE cryptoscan_app WITH LOGIN PASSWORD '<local-password>';
CREATE DATABASE cryptoscan OWNER cryptoscan_app;
```

### 2. Secrets (never commit these)

```bash
cd CEX-DEX-Parser
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Db" "Host=localhost;Port=5432;Database=cryptoscan;Username=cryptoscan_app;Password=<local-password>"
dotnet user-secrets set "Telegram:BotToken" "<bot-token>"
dotnet user-secrets set "Groq:ApiKey" "<groq-api-key>"
```

Non-secret settings stay in `appsettings.json`:

```json
{
  "ExchangeApis": {
    "Binance": "https://api.binance.com",
    "Coinbase": "https://api.exchange.coinbase.com",
    "KuCoin": "https://api.kucoin.com",
    "Bybit": "https://api.bybit.com",
    "OKX": "https://www.okx.com",
    "PancakeSwap": "https://api.dexscreener.com",
    "Hyperliquid": "https://api.hyperliquid.xyz"
  }
}
```

### 3. Migrations

```bash
dotnet ef database update      # creates all tables
```

Migrations also run automatically on app startup.

### 4. (Optional) Import legacy JSON data

Place `alerts-log.json`, `price-history.json`, and `telegram-subscribers.json` in `CEX-DEX-Parser/data-backup/` (git-ignored), then:

```bash
dotnet run -- --import-json
```

### 5. Run

```bash
dotnet run                     # backend
cd ../cex-dex-client && npm install && npm run dev   # frontend
```

The frontend runs on `http://localhost:3000` and connects to the backend URL defined in `src/api.js`.

### Working with migrations

```bash
dotnet ef migrations add <Name>   # after changing entities / AppDbContext
dotnet ef migrations script       # review SQL before applying
dotnet ef database update         # apply
```

Applied migrations are never edited — fixes go in a new migration.

### Telegram Webhook Registration

```
GET https://api.telegram.org/bot<TOKEN>/setWebhook?url=https://<your-backend>/api/telegram/webhook
```

---

## Deployment

### Backend — Azure App Service

1. Publish via Rider / Visual Studio → Azure App Service
2. Provision a hosted PostgreSQL (Azure Database for PostgreSQL, Neon, or Supabase)
3. In **Azure Portal → App Service → Settings → Environment variables**, add:

| Name | Value |
|------|-------|
| `ConnectionStrings__Db` | hosted Postgres connection string |
| `Telegram__BotToken` | bot token |
| `Telegram__ChatId` | chat id |
| `Groq__ApiKey` | Groq API key |

4. In **Configuration → General settings**: **Web sockets: On** (required for SignalR)

### Frontend — Vercel

```bash
cd cex-dex-client
vercel deploy
```

Set the production backend URL in `src/api.js` and `src/signalr.js`.

---

## Configuration Reference

| Setting | Default | Location |
|---------|---------|----------|
| Poll interval | 5 minutes | `PriceMonitorService._interval` |
| Arbitrage threshold | 3% | `ArbitrageDetector` |
| Telegram cooldown | 4 minutes | `TelegramNotifier._cooldown` |
| Database | `ConnectionStrings:Db` | user-secrets / env var |
| AI provider | Groq | `AlertInsightsService` |
| AI summary cache | `3 minutes` | `IMemoryCache` in `AlertInsightsService` |

---

## Roadmap

- [x] PostgreSQL + EF Core migrations (replacing JSON storage)
- [x] AI alert summaries via Groq
- [ ] Docker image + deployment to Azure Container Apps (Linux)
- [ ] CI/CD with GitHub Actions and EF Core migration bundles
- [ ] Order book depth metrics (spread, depth at ±0.5 / 1 / 2%)
- [ ] Slack and signed webhook alert channels
- [ ] Data retention job for old price snapshots

---

## License

MIT