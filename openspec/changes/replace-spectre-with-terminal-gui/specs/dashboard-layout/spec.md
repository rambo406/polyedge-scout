## ADDED Requirements

### Requirement: Dashboard uses Terminal.Gui Application lifecycle via AppBootstrapper
The dashboard SHALL use `AppBootstrapper` to manage Terminal.Gui's `Application.Init()` / `Application.Run()` / `Application.Shutdown()` lifecycle.

#### Scenario: Dashboard starts with Terminal.Gui initialization
- **WHEN** `AppBootstrapper.RunAsync()` is called
- **THEN** the system SHALL call `Application.Init()` to initialize the terminal driver
- **AND** construct the full view hierarchy (MenuBar, MainWindow with child Views, StatusBar)
- **AND** wire Views to their respective ViewModels
- **AND** start `DashboardViewModel.RunScanLoopAsync()` as a background task
- **AND** call `Application.Run()` to enter the event loop

#### Scenario: Dashboard shuts down cleanly
- **WHEN** the user quits the dashboard (Ctrl+Q or menu)
- **THEN** `DashboardViewModel.QuitCommand` SHALL trigger `Application.RequestStop()` to exit the event loop
- **AND** the scan loop's `CancellationToken` SHALL be cancelled
- **AND** `Application.Shutdown()` SHALL be called in a `finally` block to restore the terminal
- **AND** the terminal SHALL be fully restored (cursor visible, input mode normal) even after an unhandled exception

### Requirement: Main window contains all dashboard panels composed from Views bound to ViewModels
The dashboard SHALL display a `MainWindow` containing four child Views, each bound to its corresponding ViewModel.

#### Scenario: All panels are visible on startup
- **WHEN** the dashboard renders for the first time
- **THEN** the MainWindow SHALL contain MarketTableView (bound to MarketTableViewModel), PortfolioView (bound to PortfolioViewModel), TradesView (bound to TradesViewModel), and LogPanelView (bound to LogViewModel)
- **AND** all four panels SHALL be visible simultaneously without scrolling the main window

#### Scenario: Panels are laid out in the correct positions
- **WHEN** the dashboard renders
- **THEN** the MarketTableView SHALL occupy the top-left area (~65% width, ~60% height)
- **AND** the PortfolioView SHALL occupy the top-right area (~35% width, ~30% height)
- **AND** the TradesView SHALL occupy below the PortfolioView (~35% width, ~30% height)
- **AND** the LogPanelView SHALL occupy the bottom area (full width, ~40% height)

#### Scenario: Layout adapts to terminal size
- **WHEN** the terminal window is resized
- **THEN** all panels SHALL resize proportionally using `Pos`/`Dim` percentage-based layout
- **AND** no panel SHALL overlap another panel
- **AND** no content SHALL be clipped unless the terminal is extremely small

### Requirement: Market table displays scanner results from MarketTableViewModel
The market scanner SHALL display `MarketScanResult` data from `MarketTableViewModel` in a Terminal.Gui `TableView`.

#### Scenario: Market table shows correct columns
- **WHEN** `MarketTableViewModel.Markets` contains data
- **THEN** the MarketTableView SHALL display a `TableView` with columns: `#` (row number), `Market Question`, `YES` (price), `Volume`, `Model` (probability), `Edge`, `Action`
- **AND** the data SHALL come from `MarketScanResult` DTOs (not ad-hoc tuples)
- **AND** numeric columns SHALL be right-aligned or centered

#### Scenario: Market table shows edge-based action indicators
- **WHEN** a `MarketScanResult` has edge greater than the configured minimum edge threshold
- **THEN** the Action column SHALL show `BUY`
- **WHEN** a `MarketScanResult` has edge at or below the threshold
- **THEN** the Action column SHALL show `HOLD`

#### Scenario: Market table supports row selection
- **WHEN** the user presses `↑` or `↓` while the market table is focused
- **THEN** the `TableView`'s built-in selection SHALL move the highlighted row
- **AND** `MarketTableViewModel.SelectedIndex` SHALL be updated to match

#### Scenario: Market table is scrollable
- **WHEN** there are more markets than the visible area can display
- **THEN** the user SHALL be able to scroll through all markets using keyboard navigation
- **AND** the selected row SHALL always be visible (auto-scroll to selection)

### Requirement: MenuBar provides discoverable actions wired to DashboardViewModel commands
The dashboard SHALL include a `MenuBar` created by `MenuBarFactory` from `DashboardViewModel` commands.

#### Scenario: MenuBar contains expected menus
- **WHEN** the dashboard renders
- **THEN** the MenuBar SHALL contain: `File`, `View`, and `Trading` menus
- **AND** `File` SHALL contain `Quit` (with `Ctrl+Q` accelerator) → `DashboardViewModel.QuitCommand`
- **AND** `View` SHALL contain `Refresh` (with `F5` accelerator) → `DashboardViewModel.RefreshCommand`
- **AND** `Trading` SHALL contain `Toggle Paper/Live Mode` (with `Ctrl+T` accelerator) → `DashboardViewModel.ToggleModeCommand`
- **AND** `Trading` SHALL contain `Execute Trade` (with `Ctrl+E`) → `MarketTableViewModel.ExecuteTradeCommand`

### Requirement: StatusBar shows real-time state from DashboardViewModel
The dashboard SHALL include a `StatusBar` reflecting `DashboardViewModel` state.

#### Scenario: StatusBar displays mode
- **WHEN** `DashboardViewModel.ModeChanged` fires with paper mode
- **THEN** the StatusBar SHALL display `PAPER MODE` (updated via `Application.Invoke()`)
- **WHEN** `DashboardViewModel.ModeChanged` fires with live mode
- **THEN** the StatusBar SHALL display `LIVE MODE`

#### Scenario: StatusBar displays bankroll
- **WHEN** `PortfolioViewModel.SnapshotUpdated` fires with new bankroll value
- **THEN** the StatusBar SHALL update to show the current bankroll value

#### Scenario: StatusBar displays scan status
- **WHEN** `DashboardViewModel.ScanStatusChanged` fires with scanning status
- **THEN** the StatusBar SHALL display `Scanning...`
- **WHEN** `DashboardViewModel.ScanStatusChanged` fires with idle status
- **THEN** the StatusBar SHALL display `Idle` and the time since last scan

### Requirement: Portfolio panel shows current P&L from PortfolioViewModel
The PortfolioView SHALL display `PnlSnapshot` data from `PortfolioViewModel`.

#### Scenario: Portfolio shows all required metrics
- **WHEN** `PortfolioViewModel.Snapshot` is updated
- **THEN** the PortfolioView SHALL display: Bankroll, Open Positions count, Unrealized P&L, Realized P&L, and Total P&L
- **AND** positive P&L values SHALL be visually distinguishable from negative values (color coding)
- **AND** updates SHALL be applied via `Application.Invoke()` on the UI thread
