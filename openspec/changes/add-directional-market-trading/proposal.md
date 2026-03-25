## Why

Polymarket hosts "Up or Down" and "Higher or Lower" style directional binary markets (e.g., "Will Bitcoin go up or down between 9:45AM-10:00AM ET?") that represent a significant portion of crypto trading volume. These markets are currently silently skipped because the probability model requires a specific target price, and the `QuestionParser` has no pattern to recognise directional questions. This is a missed revenue opportunity — directional markets are simpler to model (P(price goes up) vs P(price hits X)) and often have shorter time windows with higher trading frequency.

## What Changes

- Add new regex patterns to `QuestionParser` that recognise directional market question formats: "Up or Down", "Higher or Lower", and time-windowed direction patterns (e.g., "9:45AM-10:00AM ET")
- Introduce a `ParseResult` discriminated union (or sealed hierarchy) to distinguish "price target" vs "directional" market types, replacing the current `(string?, double?)` tuple
- Add a directional probability model: uses P(price_at_T > price_now) = 1 − Φ(0) adjusted for short-term drift/momentum derived from recent price trend
- Add time-window parsing: extract start/end times from directional market questions to calculate precise `hoursLeft`
- Wire the directional model into `ProbabilityModelService` alongside the existing target-price model, dispatching based on `ParseResult` type

## Capabilities

### New Capabilities
- `directional-question-parsing`: Recognise "Up or Down", "Higher or Lower", and time-windowed directional patterns in market questions, returning a structured parse result that distinguishes directional markets from price-target markets
- `directional-probability-model`: Probability model for directional binary markets using volatility-scaled normal distribution with momentum/drift adjustment, supporting short time windows

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Domain layer**: `QuestionParser` — new patterns + new `ParseResult` return type (**BREAKING** for callers of `QuestionParser.Parse`)
- **Domain layer**: New `MomentumHelper` or extension to `MathHelper` for drift/momentum estimation
- **Application layer**: `ProbabilityModelService.CalculateProbabilityAsync` — dispatch logic for directional vs target-price markets
- **Application layer**: `IBinanceApiClient` may need a method for fetching recent klines (candlestick data) to compute momentum
- **Infrastructure layer**: Binance API client — potential new endpoint for kline/candlestick data
- **Tests**: New unit tests for directional parsing patterns, directional probability model, and integration between parser result types and model dispatch
