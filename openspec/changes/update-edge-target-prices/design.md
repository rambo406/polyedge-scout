## Context

PolyEdgeScout scans Polymarket prediction markets and calculates an "edge" — the difference between the model's estimated probability and the market's Yes price. The current formula is simply:

```
Edge = ModelProbability - YesPrice
```

This treats the model probability and market price as directly comparable scalars. For crypto price-target markets (e.g., "Will BTC hit $100K?"), the `ProbabilityModelService` already computes the model probability using current asset prices, target prices, volatility, and time to expiry. However, the edge computation downstream discards all that context and reduces it to a single subtraction.

The system currently only generates BUY or HOLD signals. The `TradeAction` enum includes `Sell`, but no code path ever produces it. The `MarketScanResult.Action` property is a raw `string` rather than the `TradeAction` enum.

## Goals / Non-Goals

**Goals:**
- Compute edges that incorporate target price context: how far the asset's current price is from the target, scaled by time and volatility, compared to what the market implies.
- Enable SELL signals when the model probability is significantly below the market price (the market is overpriced) — the trader would buy NO shares.
- Use the `TradeAction` enum throughout instead of raw strings, improving type safety.
- Keep the edge calculation in the domain `EdgeCalculation` value object so it remains testable and framework-independent.
- Ensure backtest edge calculations use the same formula as live scanning.

**Non-Goals:**
- Changing the underlying probability model (`ProbabilityModelService`) — the model probabilities stay the same.
- Implementing actual live order execution for SELL/NO trades — only paper trading for SELL.
- Adding new UI components or views — only updating the existing market table to handle SELL display.
- Adjusting Kelly criterion or bet sizing for SELL trades — bet sizing stays BUY-only for now.

## Decisions

### 1. Enhanced EdgeCalculation value object

**Decision**: Extend `EdgeCalculation` with optional `TargetPrice` and `CurrentAssetPrice` properties. When both are present, compute a target-price-weighted edge. When absent (e.g., for directional markets without a target price), fall back to the current simple formula.

**Formula for target-price-aware edge**:
```
PriceRatio = |TargetPrice - CurrentAssetPrice| / CurrentAssetPrice
Edge = (ModelProbability - MarketPrice) * (1 + PriceRatio)
```

The `PriceRatio` amplifies the raw edge by how far the current price is from the target. A market where BTC is at $60K and the target is $100K (67% away) produces a larger edge magnitude than one where BTC is at $95K targeting $100K (5% away), given the same raw probability difference. This reflects that larger price gaps represent greater mispricing opportunities.

**Alternative considered**: Using z-scores directly from the probability model. Rejected because the z-score is already baked into the model probability, and re-using it here would double-count the volatility adjustment.

### 2. Symmetric BUY/SELL/HOLD signal determination

**Decision**: Move action determination into `EdgeCalculation` as a method:
- `Edge > MinEdge` → BUY (model says more likely than market implies)
- `Edge < -MinEdge` → SELL (model says less likely than market implies, buy NO shares)
- Otherwise → HOLD

This uses the same `MinEdge` threshold symmetrically for both sides.

**Alternative considered**: Separate thresholds for BUY and SELL. Rejected for simplicity — can be added later without breaking changes.

### 3. `MarketScanResult.Action` changes to `TradeAction` enum

**Decision**: Change `Action` from `string` to the existing `TradeAction` enum. This is a **BREAKING** change but is internal — the DTO is only used within the application and console layers.

All string comparisons (`"BUY"`, `"HOLD"`) will be replaced with enum values. The `MarketTableSource` display will call `.ToString().ToUpperInvariant()` for rendering.

### 4. Passing context through to EdgeCalculation

**Decision**: `ScanOrchestrationService` will use the `ParseResult` from `QuestionParser` to extract the target price and fetch the current asset price (already available via the probability model's Binance call). Rather than duplicating API calls, the `ProbabilityModelService` will be extended to return a richer result that includes the model probability along with the current asset price and target price used in the calculation.

**Alternative considered**: Have `ScanOrchestrationService` call QuestionParser and Binance separately. Rejected because this duplicates the same parsing and API calls that `ProbabilityModelService` already makes.

### 5. New return type from ProbabilityModelService

**Decision**: Introduce a `ModelEvaluation` record in the Application DTOs layer:
```csharp
public record ModelEvaluation(
    double ModelProbability,
    double? TargetPrice,
    double? CurrentAssetPrice);
```

`ProbabilityModelService` will return `ModelEvaluation?` instead of `double?`. This avoids duplicating parsing and API calls while passing the context needed for target-price edge calculation.

The `IProbabilityModelService` interface will be updated accordingly.

## Risks / Trade-offs

- **[Breaking change]** `MarketScanResult.Action` type change → Mitigated by the fact that this DTO is internal only. All consumers in the solution will be updated in the same change.
- **[Interface change]** `IProbabilityModelService.CalculateProbabilityAsync` return type change → All implementations and callers must be updated. Only one implementation exists, so risk is low.
- **[Edge magnitude change]** The amplified edge values will be larger than current ones for markets with significant price gaps → The `MinEdge` threshold may need re-tuning, but the default 0.08 (8%) should work since the base edge `(ModelProbability - MarketPrice)` is still the dominant term.
- **[SELL signals in paper mode]** SELL trades will only work in paper mode initially → Acceptable; live SELL execution is a future scope item.
