## Context

The current dashboard in `DashboardService.cs` is a god class that mixes five concerns:

1. **Business orchestration** — `ScanLoopAsync` directly calls `IScannerService`, `IProbabilityModelService`, `IOrderService` with retry and edge-detection logic embedded in the Console layer.
2. **Keyboard input** — `HandleInputAsync` polls `Console.ReadKey` every 50ms, dispatches Q/R/T/↑/↓/Enter.
3. **UI rendering** — `RenderLoopAsync`/`RenderDashboard` constructs Spectre.Console tables/panels, clears and re-renders every 1s.
4. **Trade execution** — `ExecuteSelectedTradeAsync` invokes order placement from the UI class.
5. **Shared mutable state** — `_displayMarkets`, `_selectedRow`, `_forceRefresh`, `_displayLock` are shared fields protected by manual locking across 3 concurrent loops.

Additionally, the `MarketScanResult` DTO in the Application layer is **unused** — `DashboardService` passes ad-hoc tuples `(Market, double, double, string)` instead.

Terminal.Gui v2 provides a fundamentally different architecture: an event-driven main loop (`Application.Run()`) that handles input, rendering, and layout automatically. Background threads post UI updates via `Application.Invoke()`. Combined with MVVM, this enables a clean split of responsibilities.

## Goals / Non-Goals

**Goals:**
- Replace all Spectre.Console usage with Terminal.Gui v2
- Introduce MVVM pattern: ViewModels (POCO, testable) + Views (thin Terminal.Gui wrappers)
- Extract scan→evaluate→trade pipeline to Application layer as `IScanOrchestrationService`
- Use `MarketScanResult` DTO instead of ad-hoc tuples
- Delete `DashboardService.cs` god class
- Preserve all existing dashboard functionality (market table, portfolio, trades, log, keyboard shortcuts)
- Improve UX with flicker-free updates, native keyboard/mouse, scrollable views
- Design for future extensibility (dialogs, tabs, new panels)

**Non-Goals:**
- Changing domain entities or infrastructure services
- Adding new features beyond current dashboard functionality (future tasks)
- Supporting Terminal.Gui v1 (target v2 only)
- Adding mouse-click trading (keyboard-first; mouse is future)
- WPF-style data binding (Terminal.Gui v2 doesn't have it — use events/callbacks instead)
- Changing the persistence pattern (`OrderService` singleton + `IServiceScopeFactory` is correct)

## Decisions

### 1. Use Terminal.Gui v2 (latest stable)
**Decision:** Target Terminal.Gui v2 (`2.*` NuGet version range).
**Rationale:** v2 is a major rewrite with modern API, `Pos`/`Dim` layout system, improved `TableView`, and `Application.Invoke()` for thread-safe UI updates. v1 is legacy. The project targets .NET 10, which v2 fully supports.

### 2. MVVM pattern for the presentation layer
**Decision:** Introduce a ViewModel layer between Views and Application Services. ViewModels are POCO classes that hold UI state, expose commands, and raise events. Views subscribe to ViewModel events and update Terminal.Gui widgets.
**Rationale:** MVVM is the standard pattern for UI frameworks with event-driven architectures. It enables:
- **Testability** — ViewModels can be unit-tested without a running terminal
- **Separation of concerns** — UI rendering logic is isolated from business state management
- **Extensibility** — new Views can be added by composing existing ViewModels
- Terminal.Gui v2 lacks WPF-style data binding, so we use events/callbacks for ViewModel→View communication (this is the standard approach for Terminal.Gui MVVM)

### 3. Extract `IScanOrchestrationService` to Application layer
**Decision:** Create `IScanOrchestrationService` in the Application layer. It encapsulates the complete scan→evaluate→trade pipeline that is currently embedded in `DashboardService`.
**Rationale:** The scan pipeline is business logic, not UI logic. It calls `IScannerService`, `IProbabilityModelService`, and `IOrderService`. Moving it to the Application layer means:
- It's testable without any UI framework
- It can be reused by other consumers (e.g., a future API endpoint)
- The Console layer only handles presentation, not orchestration

### 4. ViewModels are POCO (no Terminal.Gui dependency, fully testable)
**Decision:** ViewModels must not reference `Terminal.Gui` namespaces. They use standard .NET types (`INotifyPropertyChanged`-style events, `Action<T>` callbacks, or custom event args).
**Rationale:** ViewModels should be testable with plain xUnit, without initializing a terminal. This is validated by architecture tests.

### 5. Views are thin Terminal.Gui wrappers that bind to ViewModels
**Decision:** Each View class inherits from a Terminal.Gui widget (`FrameView`, `Window`, etc.), takes a ViewModel in its constructor, subscribes to ViewModel events, and updates widgets in response. Views contain zero business logic.
**Rationale:** Views are the "dumb" rendering layer. They translate ViewModel state into Terminal.Gui widget state. This keeps Terminal.Gui concerns fully contained.

### 6. `Application.Invoke()` for thread-safe UI updates from background tasks
**Decision:** When background tasks (scan loop) update ViewModel state, the View layer uses `Application.Invoke()` to marshal widget updates onto the UI thread.
**Rationale:** Terminal.Gui is single-threaded for UI operations. `Application.Invoke()` is the thread-safe mechanism to post work to the main loop, analogous to `Dispatcher.Invoke()` in WPF.

### 7. Events/callbacks for ViewModel→View communication
**Decision:** ViewModels raise C# events (e.g., `event Action<IReadOnlyList<MarketScanResult>> MarketsUpdated`) when state changes. Views subscribe and update widgets.
**Rationale:** Terminal.Gui v2 doesn't have WPF-style data binding. Events are the idiomatic .NET alternative. They're simple, explicit, and well-supported by the testing infrastructure.

### 8. AppBootstrapper encapsulates Terminal.Gui Application lifecycle
**Decision:** Create `AppBootstrapper` class that manages `Application.Init()`, view hierarchy construction, `Application.Run()`, and `Application.Shutdown()`.
**Rationale:** Encapsulating the lifecycle in a dedicated class keeps `Program.cs` clean and makes the Terminal.Gui initialization testable/mockable. It replaces the `DashboardService.RunAsync()` entry point.

### 9. BacktestCommand uses plain Console output (no Terminal.Gui)
**Decision:** `BacktestCommand` replaces Spectre.Console with formatted `Console.WriteLine` output. It does NOT use Terminal.Gui.
**Rationale:** Terminal.Gui requires `Application.Init()` which takes over the terminal. Backtest is a one-shot command — it outputs results and exits. It uses `BacktestViewModel` for state management but renders with plain console output, making it suitable for piped/scripted use.

### 10. Database: no changes needed
**Decision:** The existing persistence pattern in `OrderService` (singleton with `IServiceScopeFactory` for scoped `DbContext` access) is preserved as-is.
**Rationale:** This pattern is correct for a long-lived in-memory service that needs periodic database access with proper scope management. No database migration or schema change is required.

## Folder Structure

```
src/PolyEdgeScout.Console/
├── Program.cs                          — DI container setup + entry point (simplified)
├── PolyEdgeScout.Console.csproj        — Package refs: Terminal.Gui v2 (Spectre.Console removed)
├── App/
│   └── AppBootstrapper.cs              — Terminal.Gui Application lifecycle
├── ViewModels/                         — Presentation state (NO Terminal.Gui references)
│   ├── DashboardViewModel.cs           — Root: scan loop, child VMs, global commands
│   ├── MarketTableViewModel.cs         — Market list, selection, trade command
│   ├── PortfolioViewModel.cs           — PnlSnapshot display state
│   ├── TradesViewModel.cs              — Trade history display state
│   ├── LogViewModel.cs                 — Log messages buffer
│   └── BacktestViewModel.cs            — Backtest execution + results
├── Views/                              — Terminal.Gui widgets (pure rendering)
│   ├── MainWindow.cs                   — Composes child views with Pos/Dim layout
│   ├── MarketTableView.cs              — FrameView + TableView for market scanner
│   ├── PortfolioView.cs                — FrameView + Labels for P&L stats
│   ├── TradesView.cs                   — FrameView + ListView for trade history
│   ├── LogPanelView.cs                 — FrameView + ListView for scrollable log
│   └── MenuBarFactory.cs               — Creates MenuBar + StatusBar from ViewModel commands
├── Commands/
│   └── BacktestCommand.cs              — Console output only, no Terminal.Gui (piped/scripted use)
└── UI/
    └── DashboardService.cs             — DELETED (split into ViewModels + Views + AppBootstrapper)

src/PolyEdgeScout.Application/
├── Interfaces/
│   ├── IScanOrchestrationService.cs    — NEW: scan→evaluate→trade pipeline contract
│   └── ...existing interfaces unchanged...
├── Services/
│   ├── ScanOrchestrationService.cs     — NEW: extracted from DashboardService
│   └── ...existing services unchanged...
├── DTOs/
│   ├── MarketScanResult.cs             — EXISTING: now actually used by the pipeline
│   └── ...existing DTOs unchanged...
```

## Layer Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Console Layer                                 │
│                                                                      │
│  ┌──────────────┐    subscribe     ┌──────────────────────┐         │
│  │    Views      │ ←──── events ───│    ViewModels         │         │
│  │ (Terminal.Gui)│                  │    (POCO)             │         │
│  │              │    invoke()      │                      │         │
│  │ MarketTable  │ ── update ──→   │ DashboardViewModel   │         │
│  │ Portfolio    │    widgets       │ MarketTableViewModel │         │
│  │ Trades       │                  │ PortfolioViewModel   │         │
│  │ LogPanel     │                  │ TradesViewModel      │         │
│  │ MainWindow   │                  │ LogViewModel         │         │
│  └──────────────┘                  └──────────┬───────────┘         │
│                                                │                     │
│         AppBootstrapper                        │ calls               │
│         (lifecycle mgmt)                       │                     │
├────────────────────────────────────────────────┼─────────────────────┤
│                    Application Layer           │                     │
│                                                ▼                     │
│                          ┌─────────────────────────────────┐        │
│                          │  IScanOrchestrationService       │        │
│                          │  ─────────────────────────────   │        │
│                          │  ScanMarketsAsync()              │        │
│                          │  → IScannerService               │        │
│                          │  → IProbabilityModelService      │        │
│                          │  → IOrderService (evaluate)      │        │
│                          │  Returns: List<MarketScanResult> │        │
│                          └──────────────┬──────────────────┘        │
│                                          │                           │
│    IOrderService  IScannerService  IProbabilityModelService         │
│    IBacktestService  IAuditService  ...                             │
├──────────────────────────────────────────┼───────────────────────────┤
│                    Domain Layer           │                           │
│                                          │                           │
│    Market  Trade  PnlSnapshot            │                           │
│    TradingMode  TradeOutcome             │                           │
├──────────────────────────────────────────┼───────────────────────────┤
│                Infrastructure Layer      │                           │
│                                          ▼                           │
│    BinanceApiClient  GammaApiClient                                 │
│    AppStateRepository  TradeRepository                              │
│    OrderService (singleton + IServiceScopeFactory)                  │
└─────────────────────────────────────────────────────────────────────┘
```

## Thread Model

### Before (Spectre.Console — 3 threads, manual locking)

```
Thread 1 (Main):       RenderLoopAsync — clear + re-render all Spectre.Console tables every 1s
Thread 2 (Background): ScanLoopAsync — polls markets, writes _displayMarkets behind lock(_displayLock)
Thread 3 (Background): HandleInputAsync — polls Console.ReadKey every 50ms
                       ↕ shared mutable state: _displayMarkets, _selectedRow, _forceRefresh
```

### After (Terminal.Gui + MVVM — 2 threads, no locks)

```
Thread 1 (Main):       Application.Run() 
                       — Terminal.Gui event loop handles rendering + input automatically
                       — Views subscribe to ViewModel events
                       — Views update widgets inside Application.Invoke() callbacks

Thread 2 (Background): DashboardViewModel.RunScanLoopAsync()
                       — Calls IScanOrchestrationService.ScanMarketsAsync()
                       — Updates ViewModel state (markets, portfolio, logs)
                       — Raises events (MarketsUpdated, PortfolioUpdated, LogAdded)
                       — Views receive events, call Application.Invoke() to update widgets
```

**Key differences:**
- No input thread — Terminal.Gui handles input in the main event loop
- No render thread — Terminal.Gui only redraws changed regions, triggered by view data changes
- No locks — cross-thread communication uses `Application.Invoke()` exclusively
- ViewModel state is updated on the background thread; widget updates are marshalled to the UI thread

### Sequence: Background Scan Update

```
Background Thread                    UI Thread (Main Loop)
─────────────                        ─────────────────────
ScanOrchestrationService
  .ScanMarketsAsync()
       │
       ▼
DashboardViewModel
  updates MarketTableVM.Markets
  updates PortfolioVM.Snapshot
  raises MarketsUpdated event ──────→  MarketTableView receives event
                                       calls Application.Invoke(() => {
                                         tableView.Table = newDataTable;
                                         tableView.SetNeedsDisplay();
                                       })
  raises PortfolioUpdated event ────→  PortfolioView receives event
                                       calls Application.Invoke(() => {
                                         bankrollLabel.Text = ...;
                                       })
  raises LogAdded event ────────────→  LogPanelView receives event
                                       calls Application.Invoke(() => {
                                         listView.SetSource(messages);
                                         listView.ScrollDown(count);
                                       })
```

## Dashboard Layout Wireframe

```
┌─ File ─┬─ View ─┬─ Trading ─────────────────────────────────────────────────┐
│                                                                              │
│ ┌─ Market Scanner ──────────────────────────┐ ┌─ Portfolio ───────────────┐  │
│ │ #  Market Question          YES Vol  Edge │ │ Bankroll:     $10,000.00 │  │
│ │ 1  Will BTC hit 80k...     0.45 $12k +0.12│ │ Open Positions: 3        │  │
│ │ 2  US Election winner...   0.62 $85k +0.08│ │ Unrealized P&L: +$142.50 │  │
│ │ 3  Fed rate cut March...   0.31 $5k  +0.05│ │ Realized P&L:   +$320.00 │  │
│ │ 4  ETH merge date...       0.78 $3k  -0.02│ │ Total P&L:     +$462.50  │  │
│ │ 5  ...                     ...  ...  ...  │ └──────────────────────────┘  │
│ │ 6  ...                     ...  ...  ...  │ ┌─ Last 5 Trades ──────────┐  │
│ │ 7  ...                     ...  ...  ...  │ │ BTC 80k...  +$50   +12%  │  │
│ │ 8  ...                     ...  ...  ...  │ │ Fed rate..  -$20    -8%  │  │
│ │                                           │ │ ETH merge.. +$35   +15%  │  │
│ └───────────────────────────────────────────┘ └──────────────────────────┘  │
│ ┌─ Log ─────────────────────────────────────────────────────────────────┐   │
│ │ [14:30:01] Scan cycle complete — 24 markets found                     │   │
│ │ [14:30:01] Edge detected: Will BTC hit 80k by April? (edge: +0.12)   │   │
│ │ [14:29:55] Paper trade executed: Fed rate cut (size: $100)            │   │
│ │ [14:29:30] Mode toggled to PAPER                                     │   │
│ └───────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ PAPER MODE | $10,000.00 | Idle | Last scan: 5s ago | F5:Refresh Ctrl+Q:Quit │
└──────────────────────────────────────────────────────────────────────────────┘
```

## View Component Breakdown

### AppBootstrapper.cs
- Injected via DI, takes `DashboardViewModel` and child ViewModels
- `RunAsync()`: calls `Application.Init()`, constructs view hierarchy, starts scan loop, calls `Application.Run()`, calls `Application.Shutdown()` in finally block
- Handles `Console.CancelKeyPress` → `Application.RequestStop()`

### MainWindow.cs
- Inherits `Window`, title: `"PolyEdge Scout v1.0"`
- Takes child Views in constructor, lays them out using `Pos`/`Dim`
- Does NOT hold business state — purely layout/composition

### MarketTableView.cs
- `FrameView` titled `"Market Scanner"` containing a `TableView`
- Subscribes to `MarketTableViewModel.MarketsUpdated` event
- Uses `DataTable` as `TableView` data source, rebuilt on each update via `Application.Invoke()`
- `CellActivated` event → calls `MarketTableViewModel.ExecuteTradeCommand()`

### PortfolioView.cs
- `FrameView` titled `"Portfolio"` with `Label` widgets
- Subscribes to `PortfolioViewModel.SnapshotUpdated` event
- Updates labels via `Application.Invoke()`

### TradesView.cs
- `FrameView` titled `"Last 5 Trades"` with `ListView`
- Subscribes to `TradesViewModel.TradesUpdated` event

### LogPanelView.cs
- `FrameView` titled `"Log"` with `ListView`
- Subscribes to `LogViewModel.MessageAdded` event
- Auto-scrolls to bottom, supports manual scrollback when focused

### MenuBarFactory.cs
- Static factory creating `MenuBar` and `StatusBar` from `DashboardViewModel` commands
- Menu items: File > Quit (`Ctrl+Q`), View > Refresh (`F5`), Trading > Toggle Mode (`Ctrl+T`), Trading > Execute Trade (`Ctrl+E`)
- StatusBar items bound to `DashboardViewModel` state (mode, bankroll, scan status)

## ViewModel Responsibilities

### DashboardViewModel (root)
- Owns child ViewModels: `MarketTableViewModel`, `PortfolioViewModel`, `TradesViewModel`, `LogViewModel`
- `RunScanLoopAsync(CancellationToken)` — calls `IScanOrchestrationService`, distributes results to child VMs
- `ToggleModeCommand` — toggles `IOrderService.PaperMode`
- `QuitCommand` — signals shutdown
- `RefreshCommand` — signals immediate scan
- Events: `ModeChanged`, `ScanStatusChanged`

### MarketTableViewModel
- `Markets` — `IReadOnlyList<MarketScanResult>` (current scan results)
- `SelectedIndex` — currently selected row
- `ExecuteTradeCommand()` — delegates to `IOrderService`
- Events: `MarketsUpdated`, `TradeExecuted`

### PortfolioViewModel
- `Snapshot` — `PnlSnapshot` (bankroll, open positions, P&L)
- Events: `SnapshotUpdated`

### TradesViewModel
- `RecentTrades` — `IReadOnlyList<Trade>` (last N trades)
- Events: `TradesUpdated`

### LogViewModel
- `Messages` — bounded buffer of log strings (e.g., last 500)
- `AddMessage(string)` — appends with timestamp
- Events: `MessageAdded`

### BacktestViewModel
- `RunBacktestAsync()` — calls `IBacktestService`
- `Results` — `BacktestResult`
- Used by `BacktestCommand` with plain console output (no Terminal.Gui)
