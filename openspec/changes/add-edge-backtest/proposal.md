## Why

The current backtest evaluates the probability model against resolved markets, but there is no way to validate whether the **edge calculation** produces profitable signals. Users cannot compare different edge formulas, select multiple crypto symbols, or compare performance across configurable timeframes. Without this, there is no empirical evidence for choosing the best edge formula or trading strategy.

## What Changes

- Add a pluggable `IEdgeFormula` system in the Domain layer, extracting the current edge calculation into `DefaultScaledEdgeFormula` and adding a `BaseEdgeFormula` alternative.
- Refactor `EdgeCalculation` value object with a factory method that accepts an `IEdgeFormula`.
- Create `EdgeBacktestService` that runs ALL registered formulas against selected crypto symbols across two user-chosen timeframes, streaming live progress.
- Compute per-formula, per-symbol, per-timeframe profitability metrics (win rate, edge accuracy, P&L, ROI).
- Multi-symbol batch selection (comma-separated BTC, ETH, SOL).
- User-selectable timeframe pair (pick 2 from 1m, 5m, 15m, 1h).
- Three-tab TUI view: Metrics (formula comparison tables + per-symbol breakdown), Charts (ASCII equity curves + win rate bars), Details (colored buy/sell signal table).
- Side-by-side timeframe comparison in each tab.

## Capabilities

### New Capabilities
- `edge-formula-system`: Domain-level pluggable edge formula interface and implementations (`IEdgeFormula`, `DefaultScaledEdgeFormula`, `BaseEdgeFormula`), with refactored `EdgeCalculation` factory method.
- `edge-backtest-engine`: Application-layer service running all registered formulas against historical data for multiple symbols and configurable timeframes, computing per-formula/per-symbol profitability metrics and streaming incremental results.
- `edge-backtest-view`: Three-tabbed TUI view with formula comparison tables, ASCII charts (equity curves + win rate bars), and colored signal details. Dual-column timeframe comparison.

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Domain layer**: New `IEdgeFormula` interface, `DefaultScaledEdgeFormula` and `BaseEdgeFormula` implementations, refactored `EdgeCalculation` with `Create` factory method and `FormulaName` property.
- **Application layer**: New `EdgeBacktestService`, DTOs for multi-formula/multi-symbol results (`EdgeBacktestEntry`, `EdgeFormulaResult`, `EdgeSymbolResult`, `EdgeBacktestTimeframeResult`, `EdgeBacktestResult`, `EdgeBacktestProgressEvent`).
- **Console layer**: New tabbed `EdgeBacktestView`, `EdgeBacktestViewModel`, custom chart views (`EquityCurveChart`, `WinRateBarChart`), `ActiveView` enum addition.
- **Infrastructure layer**: DI registration for formulas and service, verify Binance kline supports all interval options (1m, 5m, 15m, 1h).
- **Existing code**: All `EdgeCalculation` usages updated to use factory method with `DefaultScaledEdgeFormula`.
- **Tests**: Architecture tests, unit tests for formulas, service, viewmodel, updated existing `EdgeCalculation` tests.
