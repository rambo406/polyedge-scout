# PolyEdge Scout 🔍

A production-ready **Polymarket niche scanner bot** with a Terminal User Interface (TUI), built with **.NET 10** and **Spectre.Console**.

PolyEdge Scout continuously scans Polymarket for low-volume crypto micro-markets, estimates fair probabilities using real-time Binance price data and a volatility-scaled normal distribution model, identifies mispriced edges, and executes trades automatically — all from an interactive terminal dashboard.

---

## Features

| Feature | Description |
|---|---|
| **🔎 Market Scanner** | Polls the Polymarket Gamma API for active, low-volume crypto micro-markets (price milestone questions like "Will BTC hit $X by Y?") and filters by crypto keywords, recency, and volume thresholds. |
| **📊 Probability Model** | Fetches real-time price + 24h volatility from Binance and computes the probability of a token reaching a target price using a volatility-scaled Normal CDF model with a conservative fade multiplier. |
| **⚡ Edge Detection** | Compares model probability against the market's YES price to find mispriced opportunities. Only acts when edge exceeds a configurable minimum threshold (default 8%). |
| **💰 Order Execution** | Supports both **paper trading** (simulated) and **live trading** via the Polymarket CLOB API with Nethereum EIP-712 signed orders on Polygon. Uses fractional Kelly criterion for position sizing. |
| **🖥️ TUI Dashboard** | Rich terminal interface powered by Spectre.Console with real-time market table, P&L tracking, open positions, recent trades, and a live log feed — all updated every second. |
| **🧪 Backtesting** | Evaluates the probability model against historically resolved Polymarket markets, computing Brier score, calibration metrics, and hypothetical P&L. |

---

## Prerequisites

- [**.NET 10 SDK**](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)
- Internet connection for Polymarket Gamma API and Binance API access
- *(Optional)* Polygon wallet with MATIC for gas — only needed for live on-chain trading

---

## Quick Start

### 1. Clone & Restore

```bash
git clone <your-repo-url> polyedge-scout
cd polyedge-scout
dotnet restore PolyEdgeScout.slnx
```

### 2. Configure

```bash
cp .env.example .env
# Edit .env with your settings
```

#### `.env` — Environment Variables

| Variable | Description | Default |
|---|---|---|
| `PRIVATE_KEY` | Your Polygon wallet private key for signing live orders. **⚠️ NEVER commit this to source control.** | *(none)* |
| `POLYGON_RPC` | Polygon JSON-RPC endpoint URL. | `https://polygon-rpc.com` |
| `PAPER_MODE` | Set to `true` for simulated trading (no real money). Set to `false` for live on-chain execution. | `true` |

#### `appsettings.json` — Application Configuration

All settings live under the `"PolyEdgeScout"` section:

| Setting | Type | Default | Description |
|---|---|---|---|
| `PolygonRpc` | `string` | `https://polygon-rpc.com` | Polygon RPC endpoint (overridden by `POLYGON_RPC` env var). |
| `PaperMode` | `bool` | `true` | Paper trading mode (overridden by `PAPER_MODE` env var). |
| `ScanIntervalSeconds` | `int` | `60` | Seconds between market scan cycles. |
| `MaxVolume` | `double` | `3000` | Maximum market volume (USD) to consider — filters for low-volume niches. |
| `MinEdge` | `double` | `0.08` | Minimum edge (model probability − market price) required to trigger a trade (8%). |
| `DefaultBetSize` | `double` | `100` | Default bet size in USD when Kelly sizing is not applicable. |
| `KellyFraction` | `double` | `0.5` | Fractional Kelly multiplier (0.5 = half-Kelly for reduced variance). |
| `MaxBankrollPercent` | `double` | `0.02` | Maximum percentage of bankroll to risk on a single trade (2%). |
| `FadeMultiplier` | `double` | `0.92` | Conservative multiplier applied to raw model probability to reduce overconfidence. |
| `GammaApiBaseUrl` | `string` | `https://gamma-api.polymarket.com` | Polymarket Gamma API endpoint. |
| `BinanceApiBaseUrl` | `string` | `https://api.binance.com` | Binance API endpoint for price/volatility data. |
| `ClobBaseUrl` | `string` | `https://clob.polymarket.com` | Polymarket CLOB API endpoint for live order submission. |
| `LogDirectory` | `string` | `logs` | Directory for daily rotating log files. |

### 3. Build

```bash
dotnet build PolyEdgeScout.slnx
```

### 4. Run

```bash
dotnet run --project src/PolyEdgeScout.Console
```

This launches the interactive TUI dashboard. The bot will immediately begin scanning for markets, evaluating probabilities, and (in paper mode) simulating trades.

### 5. Run Backtest

```bash
dotnet run --project src/PolyEdgeScout.Console -- --backtest
```

Fetches up to 100 recently resolved Polymarket markets, filters for crypto micro-markets, runs the probability model against each, and displays calibration metrics including Brier score and hypothetical P&L.

### 6. Run Tests

```bash
dotnet test PolyEdgeScout.slnx
```

---

## Dashboard Controls

| Key | Action |
|---|---|
| `R` | Refresh — trigger an immediate market scan |
| `T` | Toggle Paper/Live trading mode |
| `Q` | Quit the application gracefully |
| `↑` / `↓` | Navigate the market list |
| `Enter` | Manually confirm a trade on the selected market |

---

## Architecture

PolyEdge Scout follows **Clean Architecture** with four layers and strict dependency flow:

```
┌─────────────────────────────────────────────────────┐
│                   Console (UI)                      │
│  Program.cs → DashboardService, BacktestCommand     │
├─────────────────────────────────────────────────────┤
│               Infrastructure                        │
│  API Clients, File Logging, DI Registration,        │
│  Configuration Loading                              │
├─────────────────────────────────────────────────────┤
│               Application                           │
│  Service Implementations, DTOs, Interfaces,         │
│  Configuration, Market Mapping                      │
├─────────────────────────────────────────────────────┤
│                   Domain                            │
│  Entities, Value Objects, Enums, Domain Services,   │
│  Core Interfaces                                    │
└─────────────────────────────────────────────────────┘

  Dependency flow: Console → Infrastructure → Application → Domain
  (Each layer only depends on the layers below it)
```

### Clean Architecture Layers

| Layer | Project | Purpose |
|---|---|---|
| **Domain** | `PolyEdgeScout.Domain` | Core business entities (`Market`, `Trade`, `PnlSnapshot`, `TradeResult`), value objects (`BetSizing`, `EdgeCalculation`), enums (`TradeAction`, `TradeStatus`), domain services (`MathHelper`, `MarketClassifier`, `QuestionParser`), and core interfaces (`ILogService`). Zero external dependencies. |
| **Application** | `PolyEdgeScout.Application` | Service interfaces (`IScannerService`, `IProbabilityModelService`, `IOrderService`, `IBacktestService`, `IBinanceApiClient`, `IGammaApiClient`, `IClobClient`), service implementations (`ScannerService`, `ProbabilityModelService`, `OrderService`, `BacktestService`), DTOs (`BinanceTickerResponse`, `GammaMarketResponse`, `MarketScanResult`, `BacktestResult`), configuration (`AppConfig`), and market mapping. Depends only on Domain. |
| **Infrastructure** | `PolyEdgeScout.Infrastructure` | External concerns — API clients (`BinanceApiClient`, `GammaApiClient`, `PolymarketClobClient`), file-based logging (`FileLogService`), configuration loading (`ConfigurationLoader`), and DI container registration (`ServiceCollectionExtensions`). Depends on Application and Domain. |
| **Console** | `PolyEdgeScout.Console` | Entry point and UI — `Program.cs` (DI host setup), `DashboardService` (Spectre.Console TUI with 3-thread architecture), and `BacktestCommand` (CLI backtest mode). Depends on all layers. |

### Key Services

| Service | Layer | Responsibility |
|---|---|---|
| **`ScannerService`** | Application | Polls the Polymarket Gamma API for active crypto micro-markets. Filters by crypto keywords (BTC, ETH, SOL, etc.), price target patterns, volume caps, and recency (last 24h). |
| **`ProbabilityModelService`** | Application | Fetches real-time price and 24h volatility from Binance. Computes the probability of a token reaching a target price using a volatility-scaled Normal CDF model. |
| **`OrderService`** | Application | Manages the full trade lifecycle — evaluation, execution (paper or live via EIP-712 signed CLOB orders), and settlement. Thread-safe in-memory ledger with bankroll tracking. |
| **`BacktestService`** | Application | Evaluates the model against historically resolved markets. Computes Brier score, calibration, and hypothetical P&L. |
| **`DashboardService`** | Console | Three-thread TUI architecture: scan loop, input loop, and render loop. Renders market tables, P&L panels, open positions, and a live log feed with Spectre.Console. |
| **`FileLogService`** | Infrastructure | Thread-safe logging to both a circular in-memory buffer (for TUI display) and daily rotating log files on disk. Supports INF/WRN/ERR/DBG levels. |
| **`MathHelper`** | Domain | Pure C# implementations of `erf` (Abramowitz & Stegun 7.1.26), `NormCdf`, `KellyFraction`, and fractional Kelly bet sizing — no external math libraries needed. |

---

## Probability Model

The core edge-detection engine uses a volatility-scaled normal distribution model:

1. **Parse** the market question to extract the crypto ticker symbol (e.g. BTC, ETH, SOL) and target price (e.g. $100,000).
2. **Fetch** the current price and 24h high/low from the Binance ticker API.
3. **Calculate 24h volatility**: `σ = (high − low) / current_price`
4. **Estimate hours remaining** until the market's deadline (`end_date_iso`).
5. **Scale volatility** to the remaining time horizon: `σ_scaled = σ × √(hours_remaining / 24)`
6. **Compute z-score**: `z = (target − current) / (current × σ_scaled)`
7. **Raw probability**: `P = 1 − Φ(z)` where Φ is the standard normal CDF (implemented via `erf`).
8. **Apply fade multiplier** (default 0.92): `P_final = P × fade` — a conservative discount to reduce model overconfidence.
9. **Calculate edge**: `edge = P_final − market_yes_price`
10. **Trade decision**: If `edge > MinEdge`, compute position size via fractional Kelly criterion and execute.

```
Probability Pipeline:

  Binance API ─→ current_price, high, low
                       │
  Market Question ─→ target_price, symbol, deadline
                       │
                  volatility = (high - low) / current
                       │
                  σ_scaled = volatility × √(hours / 24)
                       │
                  z = (target - current) / (current × σ_scaled)
                       │
                  P = (1 - Φ(z)) × fade_multiplier
                       │
                  edge = P - market_yes_price
                       │
                  edge > min_edge? → Kelly sizing → Trade
```

---

## Risk Management

| Control | Detail |
|---|---|
| **Paper mode by default** | `PAPER_MODE=true` — the bot **never** trades real money unless explicitly reconfigured. |
| **Half-Kelly sizing** | `KellyFraction=0.5` — bets half the theoretically optimal Kelly fraction to reduce variance. |
| **2% bankroll cap** | `MaxBankrollPercent=0.02` — no single trade can risk more than 2% of total bankroll. |
| **Conservative fade** | `FadeMultiplier=0.92` — raw model probabilities are discounted by 8% to account for model uncertainty. |
| **Live mode toggle** | Switching to live mode requires an explicit `T` keypress in the dashboard + confirmation. |
| **Graceful shutdown** | `Ctrl+C` triggers orderly shutdown with proper resource disposal. |

---

## ⚠️ Safety Warnings

> **This is experimental software. Use at your own risk.**

- 🔑 **NEVER** share or commit your private key. Keep `.env` in `.gitignore`.
- 🧪 **Always start with paper mode** (`PAPER_MODE=true`) to verify behavior before risking real funds.
- 💸 **Not financial advice.** This bot is a research tool, not a guaranteed profit engine. Markets are unpredictable.
- 🔬 **Model limitations.** The normal distribution model is a simplified approximation. Real crypto price movements exhibit fat tails, jumps, and regime changes not captured by this model.
- 🌐 **API dependencies.** The bot depends on Binance and Polymarket APIs which may have rate limits, downtime, or breaking changes.
- ⛽ **Gas costs.** Live trading on Polygon requires MATIC for gas fees, which are deducted from your wallet.

---

## Project Structure

```
polyedge-scout/
├── PolyEdgeScout.slnx                          # Solution file
├── appsettings.json                             # Root config (backup)
├── .env.example                                 # Template for environment variables
├── README.md
│
├── src/
│   ├── PolyEdgeScout.Domain/                    # Core domain (zero dependencies)
│   │   ├── Entities/
│   │   │   ├── Market.cs                        # Market entity
│   │   │   ├── Trade.cs                         # Trade entity
│   │   │   ├── PnlSnapshot.cs                   # P&L snapshot entity
│   │   │   └── TradeResult.cs                   # Trade result entity
│   │   ├── ValueObjects/
│   │   │   ├── BetSizing.cs                     # Kelly criterion bet sizing
│   │   │   └── EdgeCalculation.cs               # Edge calculation value object
│   │   ├── Enums/
│   │   │   ├── TradeAction.cs                   # Buy/Sell enum
│   │   │   └── TradeStatus.cs                   # Trade lifecycle status enum
│   │   ├── Services/
│   │   │   ├── MathHelper.cs                    # erf, NormCdf, Kelly implementations
│   │   │   ├── MarketClassifier.cs              # Crypto market classification
│   │   │   └── QuestionParser.cs                # Market question parsing
│   │   └── Interfaces/
│   │       └── ILogService.cs                   # Core logging interface
│   │
│   ├── PolyEdgeScout.Application/               # Business logic & interfaces
│   │   ├── Configuration/
│   │   │   └── AppConfig.cs                     # Strongly-typed configuration
│   │   ├── DTOs/
│   │   │   ├── BinanceTickerResponse.cs          # Binance API response DTO
│   │   │   ├── GammaMarketResponse.cs            # Gamma API response DTO
│   │   │   ├── MarketScanResult.cs               # Scan result DTO
│   │   │   └── BacktestResult.cs                 # Backtest result DTO
│   │   ├── Interfaces/
│   │   │   ├── IScannerService.cs                # Scanner contract
│   │   │   ├── IProbabilityModelService.cs       # Probability model contract
│   │   │   ├── IOrderService.cs                  # Order execution contract
│   │   │   ├── IBacktestService.cs               # Backtest contract
│   │   │   ├── IBinanceApiClient.cs              # Binance API client contract
│   │   │   ├── IGammaApiClient.cs                # Gamma API client contract
│   │   │   └── IClobClient.cs                    # CLOB API client contract
│   │   └── Services/
│   │       ├── ScannerService.cs                 # Market scanning implementation
│   │       ├── ProbabilityModelService.cs        # Probability model implementation
│   │       ├── OrderService.cs                   # Order execution implementation
│   │       ├── BacktestService.cs                # Backtesting implementation
│   │       └── MarketMapper.cs                   # DTO-to-entity mapping
│   │
│   ├── PolyEdgeScout.Infrastructure/            # External concerns
│   │   ├── ApiClients/
│   │   │   ├── BinanceApiClient.cs               # Binance REST API client
│   │   │   ├── GammaApiClient.cs                 # Polymarket Gamma API client
│   │   │   └── PolymarketClobClient.cs           # CLOB API + EIP-712 signing
│   │   ├── Configuration/
│   │   │   └── ConfigurationLoader.cs            # JSON + env var config loading
│   │   ├── DependencyInjection/
│   │   │   └── ServiceCollectionExtensions.cs     # DI registration
│   │   └── Logging/
│   │       └── FileLogService.cs                 # File + buffer logging
│   │
│   └── PolyEdgeScout.Console/                   # Entry point & UI
│       ├── Program.cs                            # DI host setup & entry point
│       ├── appsettings.json                      # Application configuration
│       ├── .env.example                          # Environment variable template
│       ├── Commands/
│       │   └── BacktestCommand.cs                # CLI backtest mode
│       └── UI/
│           └── DashboardService.cs               # Spectre.Console TUI dashboard
│
├── tests/
│   ├── PolyEdgeScout.Domain.Tests/              # Domain unit tests
│   ├── PolyEdgeScout.Application.Tests/         # Application service tests
│   └── PolyEdgeScout.Infrastructure.Tests/      # Infrastructure integration tests
│
├── docs/
│   └── refactoring-plan.md                      # Clean Architecture refactoring plan
│
└── logs/                                        # Daily rotating log files (auto-created)
```

---

## Dependencies

| Package | Purpose |
|---|---|
| [Spectre.Console](https://spectreconsole.net/) `0.49.*` | Rich terminal UI rendering (tables, panels, layouts) |
| [Microsoft.Extensions.Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) `10.0.*` | Configuration from JSON + environment variables |
| [Nethereum.Web3](https://nethereum.com/) `4.*` | Ethereum/Polygon Web3 integration |
| [Nethereum.Signer](https://nethereum.com/) `4.*` | EIP-712 typed data signing for CLOB orders |

---

## License

MIT
