# CEX·DEX Arbitrage Dashboard

A full-stack real-time arbitrage monitoring system that tracks price spreads across **5 centralized exchanges** and **2 decentralized exchanges**, detects arbitrage opportunities, and delivers live alerts via a React dashboard and Telegram bot.

**Live Demo:** https://cex-dex-client.vercel.app  
**Backend API:** https://cex-dex-parser-h7b3f0gwbyfah7ft.canadacentral-01.azurewebsites.net

---

## Overview

The system polls 21 trading pairs every 5 minutes across 7 exchanges, compares prices, and fires alerts whenever a spread exceeds 3%. Alerts are pushed to the React frontend in real time via SignalR WebSockets and simultaneously sent to all Telegram subscribers.

```
Exchanges → PriceMonitorService (every 5 min)
               ↓
         ArbitrageDetector (spread > 3%)
               ↓
    ┌──────────┴──────────┐
    ▼                     ▼
SignalR Hub          TelegramNotifier
(React dashboard)    (all /start subscribers)
               ↓
         JsonStorageService
         (alerts-log.json, price-history.json)
```

---

## Tech Stack

### Backend — `CEX-DEX-Parser/`
| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9 / ASP.NET Core |
| Real-time | SignalR (`AlertHub`) |
| Background work | `IHostedService` (`PriceMonitorService`) |
| Storage | JSON flat-file (`JsonStorageService`) |
| Notifications | Telegram Bot API (`Telegram.Bot`) |
| Hosting | Azure App Service (Windows) |

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

`BTC/USDT` `ETH/USDT` `SOL/USDT` `XRP/USDT` `SUI/USDT` `DOGE/USDT` `ADA/USDT` `LTC/USDT` `AVAX/USDT` `TON/USDT` `AAVE/USDT` `APEX/USDT` `ARB/USDT` `BNB/USDT` `STRK/USDT` `TRX/USDT` `PEPE/USDT` `LINK/USDT` `TRUMP/USDT` `SHIB/USDT` `XLM/USDT`

---

## Key Features

### Real-Time Price Monitoring
- Polls all 7 exchanges every **5 minutes** via `PriceMonitorService` (a .NET `BackgroundService`)
- Saves every price snapshot to `price-history.json` for historical reference
- Handles per-exchange failures gracefully — one exchange going down doesn't break the cycle

### Arbitrage Detection
- `ArbitrageDetector` compares prices across all exchange pairs for each symbol
- Fires an alert when spread exceeds **3%**
- Calculates: low exchange, high exchange, low price, high price, spread %

### Live Dashboard (React)
- Connects to the backend via **SignalR WebSocket** for zero-latency alert delivery
- Displays live price comparisons per symbol with highest/lowest exchange highlighted
- Alerts panel shows real-time incoming arbitrage opportunities
- Auto-refreshes price data every 30 seconds
- Fully responsive — works on mobile and desktop

### Telegram Bot Notifications
- Users subscribe by sending `/start` to the bot
- Chat IDs are persisted to `telegram-subscribers.json` via a webhook at `/api/telegram/webhook`
- Every arbitrage alert is broadcast to **all subscribers**
- Per-symbol **4-minute cooldown** prevents duplicate alert spam
- Alert format includes: symbol, buy exchange + price, sell exchange + price, spread %, timestamp

### Alert History
- All fired alerts are stored to `alerts-log.json`
- Accessible via `GET /api/alerts/history`
- Displayed in the frontend alert history panel

### PancakeSwap DEX Integration
- Uses **DexScreener API** to fetch BSC token prices
- Maps 18 token symbols to their verified BEP-20 contract addresses on BSC
- Automatically selects the highest-volume PancakeSwap pair for each token
- Skips tokens with no official BSC liquidity (SUI, APEX, STRK, TRUMP) rather than returning unreliable prices

---

## Project Structure

```
CEX-DEX-Parser/                  # .NET 9 backend
├── Controllers/
│   ├── AlertsController.cs      # GET /api/alerts/history
│   ├── PricesController.cs      # GET /api/prices, /api/prices/{symbol}
│   └── TelegramWebhookController.cs  # POST /api/telegram/webhook
├── Hubs/
│   └── AlertHub.cs              # SignalR hub
├── Models/
│   ├── AlertLog.cs
│   └── ExchangePrice.cs
├── Services/
│   ├── AlertService.cs          # Log + broadcast + Telegram
│   ├── ArbitrageDetector.cs     # Spread detection logic
│   ├── BinanceClient.cs
│   ├── BybitClient.cs
│   ├── ChatIdStore.cs           # Telegram subscriber persistence
│   ├── CoinbaseClient.cs
│   ├── ExchangeService.cs       # Aggregates all exchange clients
│   ├── HyperliquidClient.cs
│   ├── JsonStorageService.cs    # Thread-safe JSON flat-file storage
│   ├── KuCoinClient.cs
│   ├── OkxClient.cs
│   ├── PancakeSwapClient.cs
│   ├── PriceMonitorService.cs   # BackgroundService — main polling loop
│   └── TelegramNotifier.cs      # Broadcast to all subscribers
└── Program.cs

cex-dex-client/                  # React + Vite frontend
├── src/
│   ├── api.js                   # REST fetch helpers
│   ├── signalr.js               # SignalR connection factory
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
| `GET` | `/api/prices` | All current prices across all exchanges |
| `GET` | `/api/prices/{symbol}` | Prices for a specific symbol |
| `GET` | `/api/alerts/history` | All historical arbitrage alerts |
| `POST` | `/api/telegram/webhook` | Telegram bot webhook (receives `/start`) |
| `WS` | `/hubs/alerts` | SignalR hub — real-time alert stream |

---

## Setup & Configuration

### Prerequisites
- .NET 9 SDK
- Node.js 18+
- A Telegram bot token ([create one via @BotFather](https://t.me/BotFather))

### Backend

```bash
cd CEX-DEX-Parser
dotnet restore
dotnet run
```

Add the following to `appsettings.json` (or as environment variables for production):

```json
{
  "Telegram": {
    "BotToken": "your_bot_token_here",
    "ChatId": "your_chat_id_here"
  },
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

### Frontend

```bash
cd cex-dex-client
npm install
npm run dev
```

The frontend runs on `http://localhost:3000` by default and connects to the backend at the URL defined in `src/api.js`.

### Telegram Webhook Registration

After deploying the backend, register the webhook with Telegram once:

```
GET https://api.telegram.org/bot<TOKEN>/setWebhook?url=https://your-backend.azurewebsites.net/api/telegram/webhook
```

Or it registers automatically on startup if configured in `Program.cs`.

---

## Deployment

### Backend — Azure App Service (Windows)

1. Publish via Visual Studio → Publish → Azure App Service
2. In **Azure Portal → App Service → Settings → Environment variables**, add:

| Name | Value |
|------|-------|
| `Telegram__BotToken` | your bot token |
| `Telegram__ChatId` | your chat id |

3. In **Configuration → General settings**, enable:
   - **Web sockets: On** (required for SignalR)
   - **HTTP version: 2.0**

> Persistent storage uses `D:\home\data\cex-dex-parser` on Azure, with automatic fallback to `%TEMP%` if that path is unavailable.

### Frontend — Vercel

```bash
cd cex-dex-client
vercel deploy
```

Ensure the production backend URL is set in `src/api.js` and `src/signalr.js`.

---

## Configuration Reference

| Setting | Default | Description |
|---------|---------|-------------|
| Poll interval | 5 minutes | `PriceMonitorService._interval` |
| Arbitrage threshold | 3% | `ArbitrageDetector` |
| Telegram cooldown | 4 minutes | `TelegramNotifier._cooldown` |
| Storage path (Azure) | `D:\home\data\cex-dex-parser` | `JsonStorageService` |

---

## License

MIT
