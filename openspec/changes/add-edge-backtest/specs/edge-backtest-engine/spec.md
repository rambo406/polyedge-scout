## ADDED Requirements

### Requirement: R1 — Run all registered IEdgeFormula implementations
The system SHALL run ALL registered `IEdgeFormula` implementations against resolved markets, producing per-formula comparison results.

#### Scenario: Multiple formulas registered
- **WHEN** the edge backtest is executed with `DefaultScaledEdgeFormula` and `BaseEdgeFormula` registered
- **THEN** the system SHALL evaluate every resolved market against BOTH formulas and produce separate result sets for each

#### Scenario: Single formula registered
- **WHEN** only one `IEdgeFormula` is registered
- **THEN** the system SHALL produce results for that single formula

### Requirement: R2 — Multi-symbol filtering
The system SHALL filter resolved markets by user-selected symbols (batch, comma-separated), using the same keyword matching logic as `MarketClassifier.IsCryptoMicro`.

#### Scenario: Multiple symbols selected
- **WHEN** the user selects "BTC, ETH, SOL"
- **THEN** the system SHALL filter for markets matching ANY of the three symbols

#### Scenario: No markets found for symbols
- **WHEN** no resolved markets match the selected symbols
- **THEN** the system SHALL return an empty result set with zero metrics and log a warning

### Requirement: R3 — Dual-timeframe concurrent execution
The system SHALL run the edge backtest against two user-selected timeframes concurrently using `Task.WhenAll`.

#### Scenario: Concurrent execution
- **WHEN** the user selects "5m" and "15m" timeframes
- **THEN** the system SHALL execute both evaluations concurrently, with no shared mutable state between them

#### Scenario: Independent results
- **WHEN** both timeframe evaluations complete
- **THEN** the system SHALL return independent `EdgeBacktestTimeframeResult` for each timeframe

### Requirement: R4 — Streaming progress events
The system SHALL stream progress events per market evaluation with running metrics, raising `OnProgress` after each market is evaluated.

#### Scenario: Receiving live progress
- **WHEN** a single market is evaluated for a given formula and timeframe
- **THEN** the system SHALL emit an `EdgeBacktestProgressEvent` containing the entry, formula name, timeframe, evaluated count, total count, and running metrics

#### Scenario: Progress count accuracy
- **WHEN** N markets have been evaluated out of M total
- **THEN** the progress event SHALL report `EvaluatedCount=N` and `TotalCount=M`

### Requirement: R5 — Per-formula profitability metrics
The system SHALL compute per-formula metrics: win rate, edge accuracy, P&L (using $100 flat bet), and ROI.

#### Scenario: Computing win rate
- **WHEN** 7 out of 10 edge signals were correct for a formula
- **THEN** the win rate SHALL be 70%

#### Scenario: Computing P&L
- **WHEN** edge signals produce wins and losses with $100 flat bets
- **THEN** P&L SHALL be calculated as sum of (bet × payout - bet) for wins and (-bet) for losses

#### Scenario: Computing ROI
- **WHEN** P&L and total capital deployed are known
- **THEN** ROI SHALL be `(total P&L / total capital deployed) × 100`

### Requirement: R6 — Per-symbol breakdown
The system SHALL compute per-symbol breakdown within each formula result, including symbol name, number of markets, win rate, P&L, and ROI.

#### Scenario: Multi-symbol breakdown
- **WHEN** the backtest evaluates BTC and ETH markets
- **THEN** results SHALL include separate `EdgeSymbolResult` entries for BTC and ETH within each formula's results

### Requirement: R7 — Cancellation support
The system SHALL support cancellation via `CancellationToken`, stopping evaluation and returning partial results without throwing an exception.

#### Scenario: User cancels mid-evaluation
- **WHEN** cancellation is requested while markets are being evaluated
- **THEN** the system SHALL stop processing remaining markets and return results computed so far

#### Scenario: Partial results valid
- **WHEN** cancellation produces partial results
- **THEN** the metrics SHALL be computed only from the evaluated markets (not extrapolated)

### Requirement: R8 — Per-market error handling
The system SHALL handle errors per-market by skipping the failed market, logging the error, and continuing with remaining markets.

#### Scenario: Single market fails
- **WHEN** fetching kline data for one market fails with an exception
- **THEN** the system SHALL log the error, skip that market, and continue evaluating remaining markets

#### Scenario: All markets fail
- **WHEN** every market evaluation fails
- **THEN** the system SHALL return an empty result set with zero metrics
