## ADDED Requirements

### Requirement: Skip unparseable markets instead of assigning default probability
The `ProbabilityModelService.CalculateProbabilityAsync` method SHALL return `null` instead of `0.5` when `QuestionParser.Parse` cannot extract a token symbol or target price from the market question. The method return type SHALL be `Task<double?>` (nullable double).

#### Scenario: Unparseable market question returns null
- **WHEN** `QuestionParser.Parse` returns null for both symbol and target price for a market question like `"Will it snow in Denver?"`
- **THEN** `CalculateProbabilityAsync` SHALL return `null`

#### Scenario: Parseable market question returns calculated probability
- **WHEN** `QuestionParser.Parse` successfully extracts a symbol and target price (e.g., `"Will BTC hit $100K?"`)
- **THEN** `CalculateProbabilityAsync` SHALL return the calculated probability as before (non-null double)

#### Scenario: Binance ticker fetch failure still returns fallback
- **WHEN** `QuestionParser.Parse` succeeds but the Binance API ticker fetch fails
- **THEN** `CalculateProbabilityAsync` SHALL return `null` instead of `0.5`

### Requirement: IProbabilityModelService interface updated for nullable return
The `IProbabilityModelService` interface SHALL declare `CalculateProbabilityAsync` with return type `Task<double?>` to reflect that probability calculation can fail for non-parseable markets.

#### Scenario: Interface contract enforces nullable handling
- **WHEN** a consumer calls `IProbabilityModelService.CalculateProbabilityAsync`
- **THEN** the return type SHALL be `Task<double?>`, requiring callers to handle the null case

### Requirement: ScannerService filters out null-probability markets
The scanning pipeline SHALL skip markets for which `ProbabilityModelService` returns `null`. These markets SHALL NOT appear in scan results or generate trading signals.

#### Scenario: Null-probability market excluded from scan results
- **WHEN** `ScannerService` processes a market and `ProbabilityModelService` returns `null`
- **THEN** the market SHALL be excluded from the filtered results list

#### Scenario: Valid-probability market included in scan results
- **WHEN** `ScannerService` processes a market and `ProbabilityModelService` returns a non-null probability
- **THEN** the market SHALL be included in the filtered results list with the calculated probability
