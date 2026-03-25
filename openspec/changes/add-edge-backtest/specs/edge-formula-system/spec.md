## ADDED Requirements

### Requirement: R1 — IEdgeFormula interface
The system SHALL define an `IEdgeFormula` interface in the Domain layer (`Domain/Interfaces/`) with a `string Name { get; }` property and a `double CalculateEdge(double modelProbability, double marketPrice, double? targetPrice, double? currentAssetPrice)` method.

#### Scenario: Formula provides a name
- **WHEN** an `IEdgeFormula` implementation is instantiated
- **THEN** it SHALL expose a non-empty `Name` property identifying the formula

#### Scenario: Formula computes edge
- **WHEN** `CalculateEdge` is called with valid inputs
- **THEN** the formula SHALL return a numeric edge value representing the trading signal strength

### Requirement: R2 — DefaultScaledEdgeFormula
The system SHALL provide a `DefaultScaledEdgeFormula` class in `Domain/Services/` implementing `IEdgeFormula` that extracts the current `EdgeCalculation.Edge` logic (base edge × price ratio scaling).

#### Scenario: Computing edge with target price scaling
- **WHEN** `CalculateEdge` is called with non-null `targetPrice` and `currentAssetPrice`
- **THEN** the formula SHALL apply the existing base edge × price ratio scaling logic

#### Scenario: Computing edge without target price
- **WHEN** `CalculateEdge` is called with null `targetPrice` or null `currentAssetPrice`
- **THEN** the formula SHALL return the base edge (modelProbability - marketPrice) without scaling

### Requirement: R3 — BaseEdgeFormula
The system SHALL provide a `BaseEdgeFormula` class in `Domain/Services/` implementing `IEdgeFormula` that computes a simple `modelProbability - marketPrice` difference.

#### Scenario: Computing simple edge
- **WHEN** `CalculateEdge` is called with modelProbability=0.70 and marketPrice=0.50
- **THEN** the formula SHALL return 0.20

#### Scenario: Ignoring target/current prices
- **WHEN** `CalculateEdge` is called with any `targetPrice` and `currentAssetPrice` values
- **THEN** the formula SHALL ignore those parameters and return `modelProbability - marketPrice`

### Requirement: R4 — EdgeCalculation factory method
The system SHALL refactor the `EdgeCalculation` value object to include a `Create(IEdgeFormula formula, ...)` static factory method that accepts an `IEdgeFormula` and computes the edge at creation time.

#### Scenario: Creating EdgeCalculation via factory
- **WHEN** `EdgeCalculation.Create` is called with an `IEdgeFormula` and valid market data
- **THEN** the factory SHALL invoke `formula.CalculateEdge(...)`, store the result, and return a new `EdgeCalculation` instance

#### Scenario: Factory stores formula name
- **WHEN** `EdgeCalculation.Create` is called
- **THEN** the resulting `EdgeCalculation` SHALL have its `FormulaName` property set to the formula's `Name`

### Requirement: R5 — Edge stored as property
The `EdgeCalculation.Edge` property SHALL be stored as a value computed at creation time (via the factory method), not recomputed on every access.

#### Scenario: Accessing Edge property
- **WHEN** `EdgeCalculation.Edge` is accessed after creation
- **THEN** it SHALL return the pre-computed value without invoking any formula

### Requirement: R6 — Existing usages updated
All existing usages of `EdgeCalculation` (including `ScanOrchestrationService`, `BacktestService`, and tests) SHALL be updated to use the `EdgeCalculation.Create(formula, ...)` factory method with `DefaultScaledEdgeFormula`.

#### Scenario: ScanOrchestrationService uses factory
- **WHEN** `ScanOrchestrationService` computes an edge calculation
- **THEN** it SHALL use `EdgeCalculation.Create(defaultScaledEdgeFormula, ...)` instead of direct construction

#### Scenario: Behavioral equivalence
- **WHEN** existing code paths use the factory with `DefaultScaledEdgeFormula`
- **THEN** the computed edge values SHALL be identical to the previous implementation
