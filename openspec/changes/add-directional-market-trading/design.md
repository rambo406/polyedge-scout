## Context

The PolyEdgeScout probability pipeline currently follows a linear path: `QuestionParser.Parse(question)` returns a `(string? Symbol, double? TargetPrice)` tuple → `ProbabilityModelService` fetches the Binance ticker → applies a volatility-scaled normal-distribution model to estimate P(price hits target). Markets without a parseable target price are silently skipped.

Directional markets ("Bitcoin Up or Down 9:45AM-10:00AM ET") have no explicit target price — they ask whether the price will be higher or lower than the current price at a specific future time. These markets are structurally simpler to model (z=0 baseline) but require: (a) recognising the question pattern, (b) extracting time windows instead of price targets, and (c) a probability model that incorporates short-term momentum/drift rather than distance-to-target.

The current codebase uses .NET 8, follows clean architecture (Domain → Application → Infrastructure → Console), and the `QuestionParser` lives in the Domain layer as a pure static utility with no dependencies.

## Goals / Non-Goals

**Goals:**
- Recognise directional market questions and extract structured data (token, direction, time window)
- Provide a probability estimate for directional markets using volatility + momentum
- Maintain backward compatibility for existing price-target market flow
- Keep `QuestionParser` in the Domain layer as a pure function (no I/O)

**Non-Goals:**
- Real-time streaming price data or websocket connections (batch ticker API is sufficient)
- Machine learning or complex momentum models (simple exponential moving average is sufficient for v1)
- Supporting non-crypto directional markets (e.g., "Will the S&P 500 go up?")
- Changing the existing Kelly criterion bet sizing logic

## Decisions

### 1. Replace tuple return with sealed `ParseResult` hierarchy

**Decision**: Replace `(string?, double?)` with a sealed class hierarchy:
```
ParseResult (abstract sealed)
├── PriceTargetResult { Symbol, TargetPrice }
├── DirectionalResult { Symbol, Direction, WindowStart?, WindowEnd? }
└── UnrecognisedResult { }
```

**Rationale**: The current tuple can't express directional markets. A discriminated union (sealed hierarchy) gives pattern matching safety in C# 12, makes callers handle all cases, and is extensible for future market types.

**Alternative considered**: Adding a nullable `Direction` field to the existing tuple → rejected because it creates ambiguous states (what does `(BTC, null, Up)` mean? Is it directional or just a failed price parse?).

### 2. Directional probability model: Φ-based with momentum adjustment

**Decision**: For directional markets, compute:
```
drift = (price_now - price_lookback) / price_lookback  // momentum over lookback period
z_adjusted = drift / (scaled_volatility)               // drift-adjusted z-score
P(up) = 1 - Φ(-z_adjusted)                            // = Φ(z_adjusted)
P(down) = 1 - P(up)
```

The lookback period defaults to 1 hour of kline data. `scaled_volatility` uses the same `volatility * sqrt(hoursRatio)` formula as the existing model.

**Rationale**: This reuses the existing `MathHelper.NormCdf` and volatility scaling. The momentum adjustment captures short-term trend without introducing complex ML. At zero momentum, P(up) ≈ 0.50, which is the rational prior for a fair coin flip.

**Alternative considered**: Using RSI or MACD indicators → rejected as overengineering for v1. These can be added later as the `drift` calculation is isolated.

### 3. Time window parsing in `QuestionParser`

**Decision**: Add regex patterns for common Polymarket time formats:
- `HH:MMAM-HH:MMPM ET/EST/UTC` (e.g., "9:45AM-10:00AM ET")
- `HH:MM-HH:MM` with optional timezone
- Fallback: if no window found, use `market.EndDate`

Time windows are parsed into `TimeOnly?` start/end pairs on the `DirectionalResult`. Timezone conversion is handled in the Application layer (not Domain).

**Rationale**: Polymarket uses consistent time formats for directional markets. Parsing in Domain keeps it pure; timezone conversion in Application allows injecting IClock for testability.

### 4. Kline data for momentum via `IBinanceApiClient`

**Decision**: Add `FetchKlinesAsync(symbol, interval, limit)` to `IBinanceApiClient`. The directional model fetches the last N 5-minute candles to compute the lookback momentum.

**Rationale**: The existing `FetchTickerAsync` only provides 24h high/low. Short-term momentum requires recent price history. Binance's kline endpoint is free-tier and rate-limit-friendly.

### 5. Dispatch strategy in `ProbabilityModelService`

**Decision**: Pattern match on `ParseResult` type:
```csharp
var result = QuestionParser.Parse(market.Question);
return result switch
{
    PriceTargetResult ptr => CalculatePriceTargetProbability(ptr, market, ct),
    DirectionalResult dir => CalculateDirectionalProbability(dir, market, ct),
    UnrecognisedResult   => null  // skip
};
```

**Rationale**: Clean separation of model concerns. Each model method is independently testable. New market types in future just add a new case.

## Risks / Trade-offs

- **[Risk] Momentum signal too noisy on short windows** → Mitigation: Clamp drift contribution to ±1 standard deviation; at worst, model degrades to 50/50 which is the no-information prior.
- **[Risk] Breaking change: `QuestionParser.Parse` return type** → Mitigation: All callers are internal (only `ProbabilityModelService`). Update in same PR. Architecture tests will catch any missed references.
- **[Risk] Binance kline rate limits under heavy scanning** → Mitigation: Add simple in-memory cache (5-minute TTL, same as ticker cache). Kline data is per-symbol, not per-market.
- **[Trade-off] Sealed class hierarchy adds allocation vs tuple** → Acceptable: `Parse` is called once per market scan, not in a hot loop. Clarity > micro-optimisation.
