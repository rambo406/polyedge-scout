## 1. Application Layer: Scan Orchestration

- [x] 1.1 Create `IScanOrchestrationService` interface in `src/PolyEdgeScout.Application/Interfaces/IScanOrchestrationService.cs` — define `Task<IReadOnlyList<MarketScanResult>> ScanAndEvaluateAsync(CancellationToken ct)` and `Task<PnlSnapshot> GetPortfolioSnapshotAsync()`. This extracts the scan→evaluate→trade pipeline contract from `DashboardService`.

- [x] 1.2 Create `ScanOrchestrationService` implementation in `src/PolyEdgeScout.Application/Services/ScanOrchestrationService.cs` — inject `IScannerService`, `IProbabilityModelService`, `IOrderService`. Extract the scan loop body from `DashboardService.ScanLoopAsync()`: call scan, calculate probabilities, evaluate edges, return `List<MarketScanResult>` with all fields populated (replacing ad-hoc tuple passing). Auto-trade logic stays here (configurable).

- [x] 1.3 Register `IScanOrchestrationService` / `ScanOrchestrationService` in DI container — add to `src/PolyEdgeScout.Infrastructure/DependencyInjection/` or `Program.cs` service registration. Scoped or singleton lifetime matching existing service registrations.

## 2. Package Configuration

- [x] 2.1 Remove `Spectre.Console` package reference from `src/PolyEdgeScout.Console/PolyEdgeScout.Console.csproj` and add `Terminal.Gui` (`2.*`) package reference.

- [x] 2.2 Run `dotnet restore` and `dotnet build` to verify package resolution and that the solution compiles (existing code will have Spectre errors — that's expected, but the package graph must resolve).

## 3. ViewModels

- [x] 3.1 Create `DashboardViewModel` in `src/PolyEdgeScout.Console/ViewModels/DashboardViewModel.cs` — root ViewModel. Inject `IScanOrchestrationService`, `IOrderService`, child ViewModels. Expose `RunScanLoopAsync(CancellationToken)` (background scan loop that calls orchestration service, distributes results to child VMs, raises events), `ToggleModeCommand`, `QuitCommand`, `RefreshCommand`. Events: `ModeChanged`, `ScanStatusChanged`. Must NOT reference `Terminal.Gui`.

- [x] 3.2 Create `MarketTableViewModel` in `src/PolyEdgeScout.Console/ViewModels/MarketTableViewModel.cs` — holds `IReadOnlyList<MarketScanResult> Markets`, `int SelectedIndex`. Expose `UpdateMarkets(IReadOnlyList<MarketScanResult>)`, `ExecuteTradeCommand()`. Events: `MarketsUpdated`, `TradeExecuted`. Must NOT reference `Terminal.Gui`.

- [x] 3.3 Create `PortfolioViewModel` in `src/PolyEdgeScout.Console/ViewModels/PortfolioViewModel.cs` — holds `PnlSnapshot Snapshot`. Expose `UpdateSnapshot(PnlSnapshot)`. Event: `SnapshotUpdated`. Must NOT reference `Terminal.Gui`.

- [x] 3.4 Create `TradesViewModel` in `src/PolyEdgeScout.Console/ViewModels/TradesViewModel.cs` — holds `IReadOnlyList<Trade> RecentTrades`. Expose `UpdateTrades(IReadOnlyList<Trade>)`. Event: `TradesUpdated`. Must NOT reference `Terminal.Gui`.

- [x] 3.5 Create `LogViewModel` in `src/PolyEdgeScout.Console/ViewModels/LogViewModel.cs` — holds bounded `List<string> Messages` (max 500). Expose `AddMessage(string)` (prepends timestamp, trims buffer). Event: `MessageAdded`. Must NOT reference `Terminal.Gui`.

- [x] 3.6 Create `BacktestViewModel` in `src/PolyEdgeScout.Console/ViewModels/BacktestViewModel.cs` — inject `IBacktestService`. Expose `RunBacktestAsync(BacktestOptions)`, `BacktestResult Results`. Event: `BacktestCompleted`. Must NOT reference `Terminal.Gui`.

- [x] 3.7 Register all ViewModels in DI as transient services in `Program.cs` or DI registration module.

## 4. Views

- [x] 4.1 Create `MainWindow` in `src/PolyEdgeScout.Console/Views/MainWindow.cs` — inherits `Window`, title `"PolyEdge Scout v1.0"`. Takes child Views in constructor. Lays out: MarketTableView (top-left, ~65% width, ~60% height), PortfolioView (top-right, ~35% width, ~30% height), TradesView (below portfolio, ~35% width, ~30% height), LogPanelView (bottom, full width, ~40% height) using `Pos`/`Dim`.

- [x] 4.2 Create `MarketTableView` in `src/PolyEdgeScout.Console/Views/MarketTableView.cs` — `FrameView` titled `"Market Scanner"` containing `TableView` backed by `DataTable`. Subscribes to `MarketTableViewModel.MarketsUpdated`, rebuilds DataTable via `Application.Invoke()`. Columns: `#`, `Market Question`, `YES`, `Volume`, `Model`, `Edge`, `Action`. `CellActivated` → `MarketTableViewModel.ExecuteTradeCommand()`.

- [x] 4.3 Create `PortfolioView` in `src/PolyEdgeScout.Console/Views/PortfolioView.cs` — `FrameView` titled `"Portfolio"` with `Label` widgets for Bankroll, Open Positions, Unrealized P&L, Realized P&L, Total P&L. Subscribes to `PortfolioViewModel.SnapshotUpdated`, updates labels via `Application.Invoke()`.

- [x] 4.4 Create `TradesView` in `src/PolyEdgeScout.Console/Views/TradesView.cs` — `FrameView` titled `"Last 5 Trades"` with `ListView`. Subscribes to `TradesViewModel.TradesUpdated`, updates list via `Application.Invoke()`.

- [x] 4.5 Create `LogPanelView` in `src/PolyEdgeScout.Console/Views/LogPanelView.cs` — `FrameView` titled `"Log"` with `ListView`. Subscribes to `LogViewModel.MessageAdded`, appends and auto-scrolls via `Application.Invoke()`. Supports manual scrollback when focused.

- [x] 4.6 Create `MenuBarFactory` in `src/PolyEdgeScout.Console/Views/MenuBarFactory.cs` — static factory returning `MenuBar` (File > Quit `Ctrl+Q`, View > Refresh `F5`, Trading > Toggle Mode `Ctrl+T` / Execute Trade `Ctrl+E`) and `StatusBar` (mode, bankroll, scan status, hotkey hints). Wired to `DashboardViewModel` commands.

## 5. App Lifecycle

- [x] 5.1 Create `AppBootstrapper` in `src/PolyEdgeScout.Console/App/AppBootstrapper.cs` — injected via DI, takes `DashboardViewModel` + child ViewModels + Views. `RunAsync()`: calls `Application.Init()`, constructs MenuBar via `MenuBarFactory`, creates `MainWindow` with child Views, creates `StatusBar`, adds all to `Application.Top`, starts `DashboardViewModel.RunScanLoopAsync()` as background task, calls `Application.Run(Application.Top)`, calls `Application.Shutdown()` in finally block. Handles `Console.CancelKeyPress` → `Application.RequestStop()`.

- [x] 5.2 Rewrite `Program.cs` — simplify to: configure DI (register Application services, ViewModels, Views, AppBootstrapper), parse args. If `--backtest`: resolve `BacktestCommand`, run, exit. Otherwise: resolve `AppBootstrapper`, call `RunAsync()`. Remove all Spectre.Console references.

- [x] 5.3 Delete `src/PolyEdgeScout.Console/UI/DashboardService.cs` — all responsibilities are now split across ViewModels (state + logic), Views (rendering), AppBootstrapper (lifecycle), and `ScanOrchestrationService` (business pipeline).

## 6. BacktestCommand Refactor

- [x] 6.1 Rewrite `BacktestCommand` to use `BacktestViewModel` — inject `BacktestViewModel`, call `RunBacktestAsync()`, render results using `Console.WriteLine` with formatted string output (padded columns, summary metrics, calibration buckets). Remove all Spectre.Console types (`FigletText`, `AnsiConsole.Status`, `Table`).

- [x] 6.2 Remove all Spectre.Console `using` statements from `BacktestCommand.cs` and verify it compiles with only `System` and `Console.WriteLine`.

## 7. ViewModel Unit Tests

- [x] 7.1 Create test: `DashboardViewModel` scan loop — mock `IScanOrchestrationService` to return test `MarketScanResult` list. Verify `RunScanLoopAsync` calls orchestration service, distributes results to child VMs, raises `ScanStatusChanged` events. Verify cancellation stops the loop.

- [x] 7.2 Create test: `MarketTableViewModel` selection + trade — set `Markets` to test data, change `SelectedIndex`, call `ExecuteTradeCommand()`. Verify `MarketsUpdated` and `TradeExecuted` events fire with correct data.

- [x] 7.3 Create test: `PortfolioViewModel` snapshot update — call `UpdateSnapshot()` with test `PnlSnapshot`. Verify `SnapshotUpdated` event fires, verify `Snapshot` property reflects new values.

- [x] 7.4 Create test: `TradesViewModel` settlement — call `UpdateTrades()` with test trade list. Verify `TradesUpdated` event fires, verify `RecentTrades` contains expected items.

- [x] 7.5 Create test: `LogViewModel` message buffer — add messages up to and beyond the 500-message limit. Verify oldest messages are trimmed, `MessageAdded` event fires for each addition, messages include timestamps.

- [x] 7.6 Create test: `ScanOrchestrationService` pipeline — mock `IScannerService`, `IProbabilityModelService`, `IOrderService`. Call `ScanAndEvaluateAsync()`. Verify all three services are called in correct order, `MarketScanResult` list is populated correctly, no UI types are referenced.

## 8. README + Cleanup

- [x] 8.1 Update `README.md` — replace all references from Spectre.Console to Terminal.Gui (project description, features, architecture table, NuGet packages, directory tree). Add note about MVVM architecture in Console layer description.

- [x] 8.2 Remove old `UI/` folder contents — delete any remaining Spectre.Console rendering code, old view helpers, or formatting utilities that are no longer used.

- [x] 8.3 Verify architecture tests still pass — run `dotnet test` on `PolyEdgeScout.Architecture.Tests`. Confirm layer dependency rules pass (Console → Application → Domain is allowed; ViewModels have no Terminal.Gui dependency should be enforced by new architecture test).

## 9. Verification

- [x] 9.1 Run `dotnet build` on the full solution — verify zero errors, zero Spectre.Console references remain.

- [x] 9.2 Run `dotnet test` on all test projects — verify all existing tests pass plus new ViewModel unit tests pass.

- [x] 9.3 Manual smoke test — launch dashboard mode: verify market table renders with row selection, keyboard shortcuts work (↑/↓/Enter/F5/Ctrl+T/Ctrl+Q), log panel updates and scrolls, portfolio and trades panels update after scan, StatusBar shows correct state. Run `--backtest` mode: verify output is readable and complete.
