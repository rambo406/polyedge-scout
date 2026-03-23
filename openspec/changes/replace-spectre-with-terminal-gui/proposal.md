## Why

This change has a **dual purpose**: replace the Spectre.Console rendering library with Terminal.Gui v2 as the TUI framework, **and** refactor the Console layer from a god-class architecture to a proper **MVVM (Model-View-ViewModel)** pattern with clean layer segregation.

### Problem 1: Spectre.Console is a rendering library, not a TUI framework

The PolyEdge Scout dashboard currently uses **Spectre.Console** (`0.49.*`) for its terminal UI. Spectre.Console is fundamentally a *write-once rendering library* — it excels at formatted output (tables, panels, figlet text) but is not an interactive TUI framework. The current dashboard works around this by clearing and re-rendering the entire screen every second, manually handling cursor visibility, and polling `Console.ReadKey` on a background thread. This causes:

- **Full-screen flicker** — `AnsiConsole.Clear()` + full re-render each cycle
- **No real widget model** — "panels" are `Table`/`Panel` objects rendered to stdout, not interactive views
- **Polled input handling** — `Console.ReadKey` on a background thread with `Thread.Sleep(50)` instead of event-driven input
- **No composable layouts** — side-by-side panels use `Columns` (a rendering primitive), not a layout manager
- **No interactive features** — dialogs, scrollable regions, tabbed views, and input forms are impossible

### Problem 2: `DashboardService.cs` is a god class mixing 5 concerns

The current `DashboardService` in the Console layer conflates:

1. **Business orchestration** — The scan→evaluate→trade pipeline (`ScanLoopAsync`) calls `IScannerService`, `IProbabilityModelService`, and `IOrderService` directly, with retry logic and edge detection logic hardcoded in the UI layer.
2. **Keyboard input handling** — `HandleInputAsync` polls `Console.ReadKey` and dispatches commands.
3. **UI rendering** — `RenderLoopAsync` / `RenderDashboard` constructs Spectre.Console `Table`, `Panel`, `Columns` objects and writes them to stdout.
4. **Trade execution** — `ExecuteSelectedTradeAsync` directly invokes order placement from within the UI class.
5. **Shared mutable state** — `_displayMarkets`, `_selectedRow`, `_forceRefresh`, `_displayLock` are shared across threads, protected by manual locking.

This makes the dashboard **untestable** (cannot unit-test scan logic without a running terminal), **unextensible** (adding a new panel requires modifying the god class), and **fragile** (thread-safety depends on lock discipline across 3 concurrent loops).

### Problem 3: Unused DTOs and ad-hoc data passing

`MarketScanResult` DTO exists in the Application layer but is **unused**. Instead, `DashboardService` passes ad-hoc tuples `(Market, double, double, string)` through the scan pipeline. This means the Application layer's designed data contracts are bypassed.

## What Changes

### Terminal.Gui v2 replaces Spectre.Console
- **Remove** `Spectre.Console` (`0.49.*`) from `PolyEdgeScout.Console.csproj`
- **Add** `Terminal.Gui` (`2.*`) — a full terminal UI framework with widgets, layout, events, and thread-safe `Application.Invoke()` for background updates

### MVVM architecture replaces god class
- **ViewModels** (POCO classes, no Terminal.Gui dependency) hold presentation state and expose commands/events
- **Views** (thin Terminal.Gui wrappers) bind to ViewModels, call `Application.Invoke()` for thread-safe updates
- **`IScanOrchestrationService`** (new Application-layer service) extracts the scan→evaluate→trade pipeline from the Console layer into a properly testable service
- **`DashboardService.cs` is deleted** — its responsibilities are split across ViewModels, Views, and the new orchestration service

### Files deleted
- `src/PolyEdgeScout.Console/UI/DashboardService.cs` — replaced by ViewModels + Views

### New files (Console layer)
- `App/AppBootstrapper.cs` — Terminal.Gui `Application` lifecycle management
- `ViewModels/DashboardViewModel.cs` — root ViewModel: scan loop, child VMs, global commands
- `ViewModels/MarketTableViewModel.cs` — market list, selection, trade command  
- `ViewModels/PortfolioViewModel.cs` — P&L snapshot display state
- `ViewModels/TradesViewModel.cs` — trade history display state
- `ViewModels/LogViewModel.cs` — log message buffer
- `ViewModels/BacktestViewModel.cs` — backtest execution + results
- `Views/MainWindow.cs` — composes child views into Terminal.Gui layout
- `Views/MarketTableView.cs` — `FrameView` + `TableView` for market scanner
- `Views/PortfolioView.cs` — `FrameView` + Labels for P&L
- `Views/TradesView.cs` — `FrameView` + `ListView` for trade history
- `Views/LogPanelView.cs` — `FrameView` + `ListView` for scrollable log
- `Views/MenuBarFactory.cs` — creates `MenuBar` from ViewModel commands

### New files (Application layer)
- `Interfaces/IScanOrchestrationService.cs` — orchestration contract
- `Services/ScanOrchestrationService.cs` — extracted scan→evaluate→trade pipeline

### Modified files
- `Program.cs` — simplified DI + entry point, delegates to `AppBootstrapper`
- `PolyEdgeScout.Console.csproj` — package reference swap
- `Commands/BacktestCommand.cs` — uses `BacktestViewModel`, plain `Console.WriteLine` (no Terminal.Gui)
- `README.md` — updated references

## Impact

- **`PolyEdgeScout.Console`** — **Complete rewrite** of all UI code. `DashboardService.cs` deleted and replaced by MVVM stack (ViewModels + Views + AppBootstrapper). `BacktestCommand` rewritten to remove Spectre.Console.
- **`PolyEdgeScout.Application`** — **Minor addition**: new `IScanOrchestrationService` interface and `ScanOrchestrationService` implementation extracted from `DashboardService`. All existing interfaces and services unchanged.
- **`PolyEdgeScout.Domain`** — **No changes**. Domain entities, value objects, and services are untouched.
- **`PolyEdgeScout.Infrastructure`** — **No changes**. Persistence, API clients, and DI registration are untouched. The `OrderService` singleton pattern with `IServiceScopeFactory` for scoped DB access is correct and preserved.
- **Tests** — New ViewModel unit tests added. Architecture tests should continue to pass (they constrain layer dependencies, not UI framework choice). No existing tests reference Spectre.Console.

### NuGet Packages
- **Remove:** `Spectre.Console` `0.49.*`
- **Add:** `Terminal.Gui` `2.*`
