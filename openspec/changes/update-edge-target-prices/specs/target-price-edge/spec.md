## ADDED Requirements

### Requirement: Target-price-aware edge calculation
The `EdgeCalculation` value object SHALL compute the edge using target price context when both `TargetPrice` and `CurrentAssetPrice` are provided. The formula SHALL be:

```
PriceRatio = |TargetPrice - CurrentAssetPrice| / CurrentAssetPrice
Edge = (ModelProbability - MarketPrice) * (1 + PriceRatio)
```

When `TargetPrice` or `CurrentAssetPrice` is not available, `EdgeCalculation` SHALL fall back to the simple formula: `Edge = ModelProbability - MarketPrice`.

#### Scenario: Edge with target price far from current price
- **WHEN** `ModelProbability = 0.60`, `MarketPrice = 0.45`, `CurrentAssetPrice = 60000`, `TargetPrice = 100000`
- **THEN** `PriceRatio` SHALL be `|100000 - 60000| / 60000 ≈ 0.667`
- **AND** `Edge` SHALL be `(0.60 - 0.45) * (1 + 0.667) ≈ 0.25`

#### Scenario: Edge with target price close to current price
- **WHEN** `ModelProbability = 0.60`, `MarketPrice = 0.45`, `CurrentAssetPrice = 95000`, `TargetPrice = 100000`
- **THEN** `PriceRatio` SHALL be `|100000 - 95000| / 95000 ≈ 0.053`
- **AND** `Edge` SHALL be `(0.60 - 0.45) * (1 + 0.053) ≈ 0.158`

#### Scenario: Fallback to simple edge when no target price
- **WHEN** `ModelProbability = 0.60`, `MarketPrice = 0.45`, and `TargetPrice` is null
- **THEN** `Edge` SHALL be `0.60 - 0.45 = 0.15`

#### Scenario: Fallback to simple edge when no current asset price
- **WHEN** `ModelProbability = 0.60`, `MarketPrice = 0.45`, and `CurrentAssetPrice` is null
- **THEN** `Edge` SHALL be `0.60 - 0.45 = 0.15`

### Requirement: Symmetric BUY/SELL/HOLD action determination
The `EdgeCalculation` value object SHALL determine the trade action based on symmetric edge thresholds:
- `Edge > minEdge` → `TradeAction.Buy`
- `Edge < -minEdge` → `TradeAction.Sell`
- Otherwise → `TradeAction.Hold`

#### Scenario: Positive edge above threshold yields BUY
- **WHEN** `Edge = 0.15` and `minEdge = 0.08`
- **THEN** `DetermineAction(minEdge)` SHALL return `TradeAction.Buy`

#### Scenario: Negative edge below negative threshold yields SELL
- **WHEN** `ModelProbability = 0.30`, `MarketPrice = 0.55`, resulting in a negative edge
- **AND** the absolute edge exceeds `minEdge = 0.08`
- **THEN** `DetermineAction(minEdge)` SHALL return `TradeAction.Sell`

#### Scenario: Edge within threshold yields HOLD
- **WHEN** `Edge = 0.03` and `minEdge = 0.08`
- **THEN** `DetermineAction(minEdge)` SHALL return `TradeAction.Hold`

#### Scenario: Negative edge within threshold yields HOLD
- **WHEN** `Edge = -0.05` and `minEdge = 0.08`
- **THEN** `DetermineAction(minEdge)` SHALL return `TradeAction.Hold`

### Requirement: ModelEvaluation return type
`IProbabilityModelService.CalculateProbabilityAsync` SHALL return a `ModelEvaluation?` record instead of `double?`. The record SHALL contain:
- `ModelProbability` (double) — the computed probability
- `TargetPrice` (double?) — the target price extracted from the market question, if any
- `CurrentAssetPrice` (double?) — the current asset price fetched from Binance, if any

#### Scenario: Price target market returns full evaluation
- **WHEN** a market question is "Will BTC hit $100K?" and the current BTC price is $65,000
- **THEN** `CalculateProbabilityAsync` SHALL return a `ModelEvaluation` with `TargetPrice = 100000`, `CurrentAssetPrice = 65000`, and the computed `ModelProbability`

#### Scenario: Directional market returns evaluation without target price
- **WHEN** a market question is "Will ETH go up?" (directional, no target price)
- **THEN** `CalculateProbabilityAsync` SHALL return a `ModelEvaluation` with `TargetPrice = null` and `CurrentAssetPrice` populated from Binance ticker

#### Scenario: Unrecognised market returns null
- **WHEN** a market question cannot be parsed
- **THEN** `CalculateProbabilityAsync` SHALL return `null`

### Requirement: MarketScanResult uses TradeAction enum
`MarketScanResult.Action` SHALL be typed as `TradeAction` instead of `string`. All consumers SHALL use enum comparison instead of string literals.

#### Scenario: Scan result with BUY action
- **WHEN** a market has sufficient positive edge
- **THEN** `MarketScanResult.Action` SHALL be `TradeAction.Buy`

#### Scenario: Scan result with SELL action
- **WHEN** a market has a negative edge exceeding the threshold
- **THEN** `MarketScanResult.Action` SHALL be `TradeAction.Sell`

#### Scenario: Scan result with HOLD action
- **WHEN** a market edge is within the threshold
- **THEN** `MarketScanResult.Action` SHALL be `TradeAction.Hold`

### Requirement: ScanOrchestrationService uses target-price edge
`ScanOrchestrationService.ScanAndEvaluateAsync` SHALL use `ModelEvaluation` from the probability model to construct an `EdgeCalculation` with target price and current asset price context.

#### Scenario: Market with target price uses enhanced edge
- **WHEN** scanning a PriceTarget market (e.g., "Will BTC hit $100K?")
- **THEN** the edge SHALL be calculated using the target-price-aware formula from `EdgeCalculation`
- **AND** the action SHALL be determined by `EdgeCalculation.DetermineAction(minEdge)`

#### Scenario: Market without target price uses fallback edge
- **WHEN** scanning a directional market with no target price
- **THEN** the edge SHALL be calculated using the simple fallback formula

### Requirement: SELL signal display in market table
The `MarketTableSource` SHALL display `SELL` for markets with `TradeAction.Sell` in the Action column.

#### Scenario: Market table shows SELL action
- **WHEN** a `MarketScanResult` has `Action = TradeAction.Sell`
- **THEN** the Action column SHALL display `SELL`

#### Scenario: Market table shows BUY action
- **WHEN** a `MarketScanResult` has `Action = TradeAction.Buy`
- **THEN** the Action column SHALL display `BUY`

### Requirement: Backtest uses same edge formula
`BacktestService` edge calculation SHALL use the same `EdgeCalculation` value object and target-price-aware formula as the live scanner to ensure consistent edge measurement across live and backtest modes.

#### Scenario: Backtest entry edge matches live edge formula
- **WHEN** backtesting a price-target market
- **THEN** the `BacktestEntry.Edge` SHALL be computed using the target-price-aware `EdgeCalculation`
- **AND** the computation SHALL use the same `ModelEvaluation` context as live scanning
