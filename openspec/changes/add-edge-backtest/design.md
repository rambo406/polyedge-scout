## Context

PolyEdge Scout is a .NET Clean Architecture TUI application using Terminal.Gui v2. The app already has a `BacktestService` that evaluates the probability model against resolved Polymarket markets. However, this service tests overall model accuracy — not whether the edge calculation specifically leads to profitable trades. The `EdgeCalculation` value object computes edge with optional target-price scaling, and `BinanceApiClient` already supports fetching kline data at configurable intervals (defaulting to 5m).

The existing `BacktestView` (proposed in a separate change) provides a TUI page for running general backtests. This change introduces a focused edge profitability backtest with a pluggable formula system, multi-symbol batch selection, configurable timeframe comparison, and a three-tabbed visualization view.

## Goals / Non-Goals

**Goals:**
- Validate whether the edge calculation produces profitable trading signals using historical data
- Compare multiple edge formulas simultaneously to find the most profitable one
- Allow batch selection of multiple crypto symbols (comma-separated)
- Compare edge performance across two user-selectable timeframes side-by-side
- Stream incremental results live as markets are evaluated
- Provide ASCII visualizations: equity curves, win rate bars, signal tables
- Follow the established full-page view pattern with `TabView` for content organization

**Non-Goals:**
- Replacing or modifying the existing `BacktestService` — this is a separate, focused service
- Real money trading based on backtest results
- Persisting backtest results to disk or database
- Graphical/browser charting — ASCII only
- More than 2 timeframes simultaneously (user picks 2 from available options)

## Decisions

### 1. Pluggable IEdgeFormula system in Domain layer

**Decision:** Create an `IEdgeFormula` interface in `Domain/Interfaces/` with `string Name { get; }` and `double CalculateEdge(double modelProbability, double marketPrice, double? targetPrice, double? currentAssetPrice)`. Extract the current edge logic into `DefaultScaledEdgeFormula` and add `BaseEdgeFormula` (simple difference).

**Rationale:** Edge calculation is a core domain concept. Making it pluggable allows empirical comparison of different formulas against historical data. The interface lives in the Domain layer because edge is a domain-level computation, not an application concern.

**Alternatives considered:**
- Keep formulas in Application layer → rejected; edge is a core domain concept.
- Single formula with configurable parameters → rejected; different formulas have fundamentally different algorithms.

### 2. Refactor EdgeCalculation with factory method

**Decision:** `EdgeCalculation.Create(IEdgeFormula formula, ...)` stores the computed edge and formula name. `Edge` becomes a stored property, not recomputed on access. A `FormulaName` property tracks which formula produced the edge.

**Rationale:** Making `EdgeCalculation` formula-aware everywhere enables tracing which formula produced a given edge value. Storing the computed edge avoids repeated computation and makes the value object self-contained.

**Alternatives considered:**
- Keep `EdgeCalculation` unchanged and have formulas separate → rejected; user explicitly wants the refactoring for formula-awareness.
- Pass formula name separately → rejected; value object should be self-contained.

### 3. Run ALL registered formulas at once

**Decision:** The backtest runs every registered `IEdgeFormula` against the same market data, producing per-formula comparison results in a single run.

**Rationale:** Direct comparison is the primary goal — users want to see which formula performs better Side-by-side in the same conditions.

**Alternatives considered:**
- Run one formula at a time → rejected; direct comparison is the goal.
- Let users select which formulas to run → rejected; overhead not justified when formula count is small.

### 4. Batch multi-symbol selection

**Decision:** Comma-separated `TextField` validates each symbol against `AppConfig.MarketFilter.IncludeKeywords`. Results grouped by formula → symbol.

**Rationale:** Users want to compare edge performance across multiple assets (BTC, ETH, SOL) in a single run without restarting the backtest.

**Alternatives considered:**
- Single symbol only → rejected; user wants batch comparison.
- All symbols automatically → rejected; user wants control over which symbols are tested.

### 5. User-selectable timeframe pair

**Decision:** Two `ComboBox` selectors pick from [1m, 5m, 15m, 1h]. Results displayed side-by-side in dual-column layout.

**Rationale:** Different timeframes produce different kline data and thus different edge signals. Letting users pick any two from the available intervals provides flexibility over the locked 5m/15m approach.

**Alternatives considered:**
- Locked 5m/15m only → rejected; users want flexibility.
- N timeframes (more than 2) → rejected; complicates the UI layout; two columns is the practical limit.

### 6. Streaming results via event-driven pattern

**Decision:** `EdgeBacktestService` raises `OnProgress` event per market evaluation. ViewModel subscribes and updates the view via `Application.Invoke()` for thread-safe dispatch.

**Rationale:** The existing MVVM pattern in the app uses events for async communication. Streaming individual results as they complete provides real-time feedback during potentially long backtests. `Application.Invoke()` is the established pattern for Terminal.Gui thread safety.

**Alternatives considered:**
- `IAsyncEnumerable<T>` → simpler API but harder to integrate with Terminal.Gui's thread model.
- Polling with observable collection → more complex and not established in the codebase.

### 7. Three-tab TabView layout

**Decision:** The EdgeBacktestView uses a `TabView` with three tabs:
- **Tab 1 "Metrics"**: Config bar + formula comparison tables + per-symbol breakdown + grand totals
- **Tab 2 "Charts"**: ASCII equity curves (multi-line per formula) + win rate horizontal bar charts
- **Tab 3 "Details"**: Colored buy/sell signal table with ✅/❌ indicators, scrollable, filterable

**Rationale:** Separating concerns into tabs keeps each view focused. Metrics for quick comparison, Charts for visual analysis, Details for signal-level inspection. TabView is the established Terminal.Gui v2 pattern for multi-content views.

**Alternatives considered:**
- Single scrollable view → rejected; too much content, poor UX.
- Two tabs (metrics + details) → rejected; charts deserve their own tab.

### 8. Dual-column per tab for timeframe comparison

**Decision:** Each tab maintains side-by-side timeframe comparison: left column = timeframe A, right column = timeframe B.

**Rationale:** Side-by-side comparison is the most intuitive way to evaluate which timeframe produces better signals. Having this in every tab provides consistent layout.

**Alternatives considered:**
- Tabbed timeframe switching → loses the comparison aspect.
- Single column with timeframe rows → cluttered for tables and charts.

### 9. Concurrent timeframe execution

**Decision:** Execute both timeframe backtests concurrently using `Task.WhenAll`. No shared mutable state between timeframe executions.

**Rationale:** Both timeframes evaluate the same markets but with different kline data. Concurrent execution halves runtime. Independent state eliminates concurrency issues.

**Alternatives considered:**
- Sequential execution → simpler but doubles runtime unnecessarily.

### 10. Navigation shortcut: Ctrl+E

**Decision:** Register `Ctrl+E` in `MainWindow.OnKeyDown` to navigate to EdgeBacktestView.

**Rationale:** `E` for Edge backtest is mnemonic. Current shortcuts: `Ctrl+L` (Log), `Ctrl+O` (Trades), `Ctrl+B` (Backtest — proposed), `Ctrl+T` (Toggle), `Ctrl+Q` (Quit), `Ctrl+Shift+R` (Reset). No conflict.

## Risks / Trade-offs

- **[Risk] Rate limiting with multi-symbol + multi-formula** → Increases API calls compared to single-symbol. Mitigated by existing rate limit handling in `BinanceApiClient` and ≤30 markets limit per backtest.
- **[Risk] Custom ASCII chart views are non-trivial** → `EquityCurveChart` and `WinRateBarChart` need careful Unicode block character handling and Terminal.Gui v2 drawing APIs. Mitigated by keeping chart logic simple (block characters, no anti-aliasing).
- **[Risk] Refactoring EdgeCalculation touches existing code paths** → All current usages of `EdgeCalculation` must be updated to use the factory method. Mitigated by comprehensive unit tests verifying behavioral equivalence.
- **[Risk] High complexity (~38 tasks across 8 groups)** → Mitigated by clear task ordering with dependency chains and independent groups that can be parallelized.
- **[Trade-off] Singleton ViewModel** → Same as BacktestViewModel: preserves results across navigation but holds data for app lifetime. Acceptable given bounded result set size.
- **[Trade-off] Two concurrent API call streams per timeframe** → Doubles API traffic but halves user wait time. Acceptable trade-off for better UX.
- **[Risk] No resolved 15m/1h markets in Binance** → The Gamma API fetches resolved markets generically. The timeframe applies to kline data from Binance, not market resolution. Need to verify Binance supports all four interval options (1m, 5m, 15m, 1h).
