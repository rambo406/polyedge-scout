## ADDED Requirements

### Requirement: ViewModels must not reference Terminal.Gui
All ViewModel classes in the `ViewModels/` folder SHALL be POCO classes with no dependency on `Terminal.Gui` namespaces, types, or assemblies.

#### Scenario: ViewModel files have no Terminal.Gui using statements
- **WHEN** any ViewModel file (`DashboardViewModel.cs`, `MarketTableViewModel.cs`, `PortfolioViewModel.cs`, `TradesViewModel.cs`, `LogViewModel.cs`, `BacktestViewModel.cs`) is inspected
- **THEN** it SHALL NOT contain any `using Terminal.Gui` statements
- **AND** it SHALL NOT reference any `Terminal.Gui` types (e.g., `Application`, `View`, `TableView`, `FrameView`)

#### Scenario: ViewModels are testable without Terminal.Gui initialization
- **WHEN** a ViewModel is instantiated in a unit test
- **THEN** it SHALL NOT require `Application.Init()` to be called
- **AND** all ViewModel methods SHALL execute without a running terminal
- **AND** all ViewModel events SHALL fire correctly in a test environment

#### Scenario: Architecture test enforces ViewModel isolation
- **WHEN** architecture tests run
- **THEN** there SHALL be a test verifying that types in the `PolyEdgeScout.Console.ViewModels` namespace do not depend on `Terminal.Gui`

### Requirement: Views must not contain business logic
All View classes in the `Views/` folder SHALL contain only UI rendering and event wiring code — no business logic, service calls, or state management.

#### Scenario: Views do not call Application services directly
- **WHEN** any View file is inspected
- **THEN** it SHALL NOT reference `IScannerService`, `IProbabilityModelService`, `IOrderService`, `IScanOrchestrationService`, or any Application-layer interface
- **AND** it SHALL NOT perform any business calculations

#### Scenario: Views delegate all actions to ViewModel commands
- **WHEN** a user action occurs in a View (e.g., `CellActivated` on `TableView`, menu item clicked)
- **THEN** the View SHALL call the corresponding ViewModel method or command
- **AND** the View SHALL NOT implement any logic to decide what the action means

#### Scenario: Views only update widgets in response to ViewModel events
- **WHEN** data needs to be displayed
- **THEN** the View SHALL receive the data via a ViewModel event subscription
- **AND** the View SHALL update Terminal.Gui widgets inside an `Application.Invoke()` callback
- **AND** the View SHALL NOT fetch data from any source

### Requirement: IScanOrchestrationService must be testable without UI
The `IScanOrchestrationService` in the Application layer SHALL be fully testable with mocked dependencies and no UI framework.

#### Scenario: Orchestration service has no Console-layer dependencies  
- **WHEN** `ScanOrchestrationService` is inspected
- **THEN** it SHALL NOT reference `Terminal.Gui`, `System.Console`, or any type from `PolyEdgeScout.Console`
- **AND** it SHALL only depend on Application-layer interfaces and Domain types

#### Scenario: Orchestration service is testable with mocks
- **WHEN** `ScanOrchestrationService` is instantiated in a unit test
- **THEN** `IScannerService`, `IProbabilityModelService`, and `IOrderService` SHALL be injectable as mocks
- **AND** `ScanAndEvaluateAsync()` SHALL return `IReadOnlyList<MarketScanResult>` with correct data from mocked services
- **AND** the test SHALL verify the scan→evaluate→trade pipeline order

### Requirement: All ViewModel state changes must raise events for Views to observe
ViewModels SHALL expose C# events for every state change that Views need to render.

#### Scenario: MarketTableViewModel raises MarketsUpdated
- **WHEN** `MarketTableViewModel.UpdateMarkets()` is called with new scan results
- **THEN** `MarketTableViewModel.MarketsUpdated` event SHALL fire
- **AND** the event SHALL provide the updated `IReadOnlyList<MarketScanResult>`

#### Scenario: PortfolioViewModel raises SnapshotUpdated
- **WHEN** `PortfolioViewModel.UpdateSnapshot()` is called with a new `PnlSnapshot`
- **THEN** `PortfolioViewModel.SnapshotUpdated` event SHALL fire

#### Scenario: TradesViewModel raises TradesUpdated
- **WHEN** `TradesViewModel.UpdateTrades()` is called with a new trade list
- **THEN** `TradesViewModel.TradesUpdated` event SHALL fire

#### Scenario: LogViewModel raises MessageAdded
- **WHEN** `LogViewModel.AddMessage()` is called
- **THEN** `LogViewModel.MessageAdded` event SHALL fire with the new message

#### Scenario: DashboardViewModel raises ModeChanged and ScanStatusChanged
- **WHEN** `DashboardViewModel.ToggleModeCommand` is executed
- **THEN** `DashboardViewModel.ModeChanged` event SHALL fire
- **WHEN** the scan loop begins or completes a cycle
- **THEN** `DashboardViewModel.ScanStatusChanged` event SHALL fire with the current status

### Requirement: MarketScanResult DTO must be used (not ad-hoc tuples)
The scan pipeline SHALL use the existing `MarketScanResult` DTO from the Application layer for all market data passing between services, ViewModels, and Views.

#### Scenario: ScanOrchestrationService returns MarketScanResult
- **WHEN** `IScanOrchestrationService.ScanAndEvaluateAsync()` completes
- **THEN** it SHALL return `IReadOnlyList<MarketScanResult>` with all fields populated:
  - `Market` (domain entity reference)
  - `YesPrice` (decimal)
  - `Volume` (decimal)
  - `ModelProbability` (double)
  - `Edge` (double)
  - `Action` (string: "BUY" or "HOLD")

#### Scenario: ViewModels use MarketScanResult (not tuples)
- **WHEN** `MarketTableViewModel.Markets` is accessed
- **THEN** it SHALL be typed as `IReadOnlyList<MarketScanResult>`
- **AND** no `(Market, double, double, string)` tuples or anonymous types SHALL appear in ViewModel code

#### Scenario: Views consume MarketScanResult for display
- **WHEN** `MarketTableView` rebuilds its `DataTable`
- **THEN** it SHALL read properties from `MarketScanResult` objects
- **AND** the `DataTable` columns SHALL map to `MarketScanResult` properties

### Requirement: Clean layer dependency direction
The MVVM architecture SHALL maintain strict unidirectional dependencies: Views → ViewModels → Application Services → Domain.

#### Scenario: No circular dependencies
- **WHEN** the dependency graph is analyzed
- **THEN** Views SHALL depend on ViewModels (constructor injection or event subscription)
- **AND** ViewModels SHALL depend on Application-layer interfaces (constructor injection)
- **AND** Application Services SHALL depend on Domain types and other Application interfaces
- **AND** no reverse dependencies SHALL exist (ViewModels SHALL NOT depend on Views, Application SHALL NOT depend on Console)

#### Scenario: Architecture tests validate layer boundaries
- **WHEN** architecture tests run
- **THEN** the existing `LayerDependencyTests` SHALL pass (Application → Domain, Console → Application → Domain)
- **AND** a new test SHALL verify ViewModels do not reference Terminal.Gui
- **AND** a new test SHALL verify Views do not reference Application-layer service interfaces
