## Why

The current edge calculation uses a naive formula (`Edge = ModelProbability - YesPrice`) that treats model probability and market price as directly comparable. This works for simple binary markets but ignores the relationship between the asset's current price, target price, and the market's Yes price. By incorporating target prices into the edge calculation, we can produce more meaningful edge values that account for how far the current asset price is from the target—giving traders a better signal for BUY, SELL, and HOLD decisions.

Currently the system only ever recommends BUY or HOLD. The `TradeAction` enum already defines `Sell`, but the edge logic never produces a SELL signal. With target-price-aware edges, we can detect when the market is overpriced relative to the model and generate SELL signals (buy NO shares).

## What Changes

- **Update `EdgeCalculation` value object** to accept a target price and current asset price, computing a target-price-aware edge that measures how much the market misprices the probability given the distance between current and target prices.
- **Extend `ScanOrchestrationService`** to pass target price and current price context through to the edge calculation, enabling richer edge signals.
- **Introduce SELL signal logic** in `ScanOrchestrationService` and `OrderService` — when the model probability is significantly lower than the market's Yes price (negative edge exceeds threshold), recommend SELL (buy NO shares).
- **Update `MarketScanResult` DTO** to use the `TradeAction` enum instead of a raw string for the `Action` field. **BREAKING** for any code that compares `Action` to string literals.
- **Update `MarketTableSource`** display to show SELL signals and render them distinctly.
- **Extend `BacktestService`** edge calculation to use the same target-price-aware formula for consistent backtesting.

## Capabilities

### New Capabilities
- `target-price-edge`: Target-price-aware edge calculation logic that replaces the simple probability-minus-price formula with one that incorporates the asset's current price relative to its target price, and supports BUY/SELL/HOLD action determination.

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Domain layer**: `EdgeCalculation` value object gains new properties and a revised `Edge` formula.
- **Application layer**: `ScanOrchestrationService`, `OrderService`, and `BacktestService` updated to pass additional context and use new edge logic. `MarketScanResult.Action` changes from `string` to `TradeAction` enum.
- **Console layer**: `MarketTableSource` and `MarketTableViewModel` updated for enum-based action and SELL display.
- **Tests**: Existing tests referencing `Action = "BUY"` / `"HOLD"` strings must be updated to use `TradeAction` enum values. New tests needed for SELL signals and target-price edge cases.
