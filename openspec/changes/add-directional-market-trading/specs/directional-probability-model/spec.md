## ADDED Requirements

### Requirement: Calculate directional probability using volatility and momentum
The directional probability model SHALL estimate the probability that a cryptocurrency price will be higher (or lower) than the current price at the end of a time window, using volatility-scaled normal distribution adjusted for short-term momentum.

#### Scenario: Zero momentum (no recent trend)
- **WHEN** the recent price trend shows zero drift (price unchanged over lookback period)
- **THEN** the model SHALL return approximately 0.50 probability for the "up" direction

#### Scenario: Positive momentum
- **WHEN** the recent price trend shows positive drift (price increased over lookback period)
- **THEN** the model SHALL return a probability greater than 0.50 for the "up" direction

#### Scenario: Negative momentum
- **WHEN** the recent price trend shows negative drift (price decreased over lookback period)
- **THEN** the model SHALL return a probability less than 0.50 for the "up" direction

#### Scenario: Drift clamping prevents extreme estimates
- **WHEN** the momentum signal is extremely large (e.g., flash crash or pump)
- **THEN** the model SHALL clamp the drift contribution to ±1 standard deviation, preventing probability from exceeding the configured bounds

### Requirement: Scale volatility to the directional market time window
The directional probability model SHALL scale volatility proportionally to the square root of the time window duration, using the same formula as the existing price-target model.

#### Scenario: Short time window (15 minutes)
- **WHEN** a directional market has a 15-minute window
- **THEN** the volatility SHALL be scaled using `sqrt(0.25 / 24)` hours ratio

#### Scenario: Longer time window (1 hour)
- **WHEN** a directional market has a 1-hour window
- **THEN** the volatility SHALL be scaled using `sqrt(1.0 / 24)` hours ratio

#### Scenario: No explicit time window — fallback to market EndDate
- **WHEN** the directional market has no parsed time window but has an `EndDate`
- **THEN** the model SHALL calculate hours remaining from `DateTime.UtcNow` to `EndDate`

### Requirement: Dispatch to correct probability model based on ParseResult type
The `ProbabilityModelService` SHALL dispatch to the appropriate probability model based on the `ParseResult` type returned by `QuestionParser`.

#### Scenario: PriceTargetResult dispatches to existing model
- **WHEN** the parser returns a `PriceTargetResult`
- **THEN** `ProbabilityModelService` SHALL use the existing volatility-scaled target-price probability calculation

#### Scenario: DirectionalResult dispatches to directional model
- **WHEN** the parser returns a `DirectionalResult`
- **THEN** `ProbabilityModelService` SHALL use the directional probability model with momentum adjustment

#### Scenario: UnrecognisedResult returns null
- **WHEN** the parser returns an `UnrecognisedResult`
- **THEN** `ProbabilityModelService` SHALL log a debug message and return null

### Requirement: Fetch kline data for momentum calculation
The system SHALL fetch recent candlestick (kline) data from Binance to compute the short-term momentum used by the directional probability model.

#### Scenario: Successful kline fetch
- **WHEN** the directional model requests momentum data for a valid symbol
- **THEN** the system SHALL fetch the last N 5-minute candles and compute drift as `(close_last - close_first) / close_first`

#### Scenario: Kline fetch failure — graceful degradation
- **WHEN** the kline API call fails or returns no data
- **THEN** the model SHALL fall back to zero drift (momentum = 0), producing a ~50% probability estimate
