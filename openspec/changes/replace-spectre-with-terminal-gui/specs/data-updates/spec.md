## ADDED Requirements

### Requirement: ViewModel-driven data updates replace direct widget manipulation
All data updates to Terminal.Gui Views SHALL originate from ViewModel state changes. Views subscribe to ViewModel events and update widgets — no service or business logic may update widgets directly.

#### Scenario: Scan results flow through ViewModel layer
- **WHEN** the background scan loop completes a cycle
- **THEN** `DashboardViewModel` SHALL call `IScanOrchestrationService.ScanAndEvaluateAsync()`
- **AND** distribute `List<MarketScanResult>` results to `MarketTableViewModel.UpdateMarkets()`
- **AND** distribute `PnlSnapshot` to `PortfolioViewModel.UpdateSnapshot()`
- **AND** distribute recent trades to `TradesViewModel.UpdateTrades()`
- **AND** add log messages to `LogViewModel.AddMessage()`
- **AND** ViewModels SHALL raise their respective events (`MarketsUpdated`, `SnapshotUpdated`, `TradesUpdated`, `MessageAdded`)

#### Scenario: Views update widgets only in response to ViewModel events
- **WHEN** `MarketTableViewModel.MarketsUpdated` fires
- **THEN** `MarketTableView` SHALL call `Application.Invoke()` to rebuild the `DataTable` and update the `TableView`
- **AND** the update SHALL execute on the UI thread
- **AND** no other code path SHALL directly modify the `TableView` data source

#### Scenario: All ViewModel events are thread-safe
- **WHEN** a ViewModel raises an event from the background scan thread
- **THEN** Views SHALL always marshal widget updates via `Application.Invoke()`
- **AND** no lock contention SHALL occur between the scan loop and the UI thread
- **AND** no shared mutable fields SHALL exist for cross-thread UI state

### Requirement: Batched updates within a single scan cycle
When the scan loop completes, all related ViewModel updates SHALL happen in the same cycle, and Views SHALL batch their widget updates.

#### Scenario: Market + portfolio + trades + log update in one cycle
- **WHEN** `DashboardViewModel` receives scan results
- **THEN** it SHALL update all child ViewModels before returning
- **AND** each View SHALL independently receive its ViewModel's event
- **AND** each View SHALL call `Application.Invoke()` to update its widgets
- **AND** Terminal.Gui SHALL coalesce the redraws into a single screen update

### Requirement: StatusBar updates reflect ViewModel state
The StatusBar SHALL update only in response to `DashboardViewModel` and `PortfolioViewModel` state changes.

#### Scenario: Scan status updates from DashboardViewModel
- **WHEN** `DashboardViewModel.ScanStatusChanged` fires with "Scanning"
- **THEN** the StatusBar SHALL update to show `Scanning...` via `Application.Invoke()`
- **WHEN** `DashboardViewModel.ScanStatusChanged` fires with "Idle"
- **THEN** the StatusBar SHALL update to show `Idle` and elapsed time since last scan

#### Scenario: Mode change updates from DashboardViewModel
- **WHEN** `DashboardViewModel.ModeChanged` fires
- **THEN** the StatusBar mode indicator SHALL update via `Application.Invoke()`

#### Scenario: Bankroll updates from PortfolioViewModel
- **WHEN** `PortfolioViewModel.SnapshotUpdated` fires
- **THEN** the StatusBar bankroll display SHALL update via `Application.Invoke()`

### Requirement: Log messages update in real-time via LogViewModel
Log messages from any source SHALL flow through `LogViewModel` and appear in `LogPanelView` promptly.

#### Scenario: Log entry from scan loop
- **WHEN** the scan loop logs a message (e.g., "Scan cycle complete — 24 markets found")
- **THEN** `DashboardViewModel` SHALL call `LogViewModel.AddMessage()`
- **AND** `LogViewModel.MessageAdded` SHALL fire
- **AND** `LogPanelView` SHALL receive the event and update the `ListView` via `Application.Invoke()`
- **AND** the log view SHALL auto-scroll to the bottom

#### Scenario: Log entry from trade execution
- **WHEN** `MarketTableViewModel.ExecuteTradeCommand()` completes
- **THEN** the trade outcome message SHALL be added via `LogViewModel.AddMessage()`
- **AND** the update SHALL appear in `LogPanelView` via the event → `Application.Invoke()` pipeline

### Requirement: Manual refresh triggers immediate scan via ViewModel command
When the user requests a manual refresh, `DashboardViewModel.RefreshCommand` SHALL signal an immediate scan cycle.

#### Scenario: F5 triggers immediate scan through ViewModel
- **WHEN** the user presses `F5`
- **THEN** `DashboardViewModel.RefreshCommand` SHALL signal the scan loop to skip the wait interval
- **AND** `DashboardViewModel.ScanStatusChanged` SHALL fire with scanning status
- **AND** `LogViewModel.AddMessage()` SHALL record `Manual refresh triggered`
- **AND** the scan loop SHALL begin within 250ms

### Requirement: Trade execution updates UI through ViewModel events
When a trade is executed, the UI update SHALL flow through the ViewModel event pipeline, not through direct widget manipulation.

#### Scenario: Manual trade updates all ViewModels
- **WHEN** the user executes a trade via `MarketTableViewModel.ExecuteTradeCommand()`
- **THEN** `MarketTableViewModel.TradeExecuted` SHALL fire
- **AND** `DashboardViewModel` SHALL refresh `PortfolioViewModel` and `TradesViewModel` with updated data
- **AND** each View SHALL receive its ViewModel's event and update via `Application.Invoke()`

#### Scenario: Auto-trade from scan loop updates through same pipeline
- **WHEN** the scan loop auto-executes a trade via `IScanOrchestrationService`
- **THEN** the trade result SHALL be included in the scan cycle's ViewModel updates
- **AND** all ViewModels SHALL be updated in the same cycle
- **AND** all Views SHALL receive events and update via `Application.Invoke()`

### Requirement: Scan loop continues on background thread
The scan loop SHALL run on a background thread via `DashboardViewModel.RunScanLoopAsync()`, independent of the Terminal.Gui main loop.

#### Scenario: Scan loop runs concurrently with UI
- **WHEN** the dashboard is running
- **THEN** `DashboardViewModel.RunScanLoopAsync()` SHALL call `IScanOrchestrationService` on a background thread
- **AND** the Terminal.Gui main loop SHALL remain responsive to input during scanning
- **AND** the scan interval SHALL be respected (configurable via `AppConfig.ScanIntervalSeconds`)

#### Scenario: Scan loop respects cancellation
- **WHEN** `DashboardViewModel.QuitCommand` is invoked
- **THEN** the scan loop's `CancellationToken` SHALL be cancelled
- **AND** the scan loop SHALL exit within one scan interval

## MODIFIED Requirements

### Requirement: Scan pipeline logic preserved, extracted to Application layer
The scan loop's business logic (market scanning, probability calculation, edge detection, auto-trading) is **preserved identically** but **moved to `ScanOrchestrationService`** in the Application layer. `DashboardViewModel` calls the orchestration service instead of calling individual services directly.

#### Scenario: Orchestration service encapsulates pipeline
- **WHEN** `DashboardViewModel.RunScanLoopAsync()` executes a cycle
- **THEN** it SHALL call `IScanOrchestrationService.ScanAndEvaluateAsync()`
- **AND** the orchestration service SHALL call `IScannerService`, `IProbabilityModelService`, and `IOrderService` internally
- **AND** it SHALL return `IReadOnlyList<MarketScanResult>` with all fields populated from DOM DTOs

## REMOVED Requirements

### Requirement: Screen clear and re-render every 1 second
The `RenderLoopAsync` that called `AnsiConsole.Clear()` and re-rendered all Spectre.Console primitives every 1 second is removed. Terminal.Gui renders automatically when View widgets are updated via ViewModel events.

### Requirement: Shared mutable state with lock-based synchronization
The `_displayMarkets`, `_selectedRow`, `_forceRefresh` fields protected by `lock(_displayLock)` are removed. All cross-thread communication uses ViewModel events + `Application.Invoke()`.

### Requirement: Direct widget manipulation from business logic
No service or business logic code may directly call Terminal.Gui widget methods. All updates flow through ViewModels.
