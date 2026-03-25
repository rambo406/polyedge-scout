## Context

PolyEdge Scout is a .NET Clean Architecture TUI application using Terminal.Gui v2. The app already supports full-page views via `ViewNavigator` — the pattern is established with `FullLogView` and `TradesManagementView`. Both are `FrameView` subclasses that implement `IShortcutHelpProvider`, register with `ViewNavigator`, and use `Escape` to return to the dashboard.

The backtest infrastructure exists at the application layer (`BacktestService`, `BacktestResult`, `BacktestEntry`, `CalibrationBucket`) and is exposed via `BacktestViewModel` in the console layer. Currently, backtests are only accessible through the CLI `BacktestCommand`, requiring users to exit the TUI.

## Goals / Non-Goals

**Goals:**
- Provide a full-page `BacktestView` accessible via `Ctrl+B` from the dashboard
- Display backtest summary metrics (Brier score, win rate, edge accuracy, hypothetical P&L)
- Show individual market entry results in a scrollable table
- Allow triggering a backtest run from within the view with progress feedback
- Follow the established full-page view pattern exactly (FrameView, IShortcutHelpProvider, ViewNavigator integration)
- Retain backtest results across navigation (singleton VM lifecycle)

**Non-Goals:**
- Backtest history / multiple run comparison (future enhancement)
- Custom backtest parameters (market count, filters) from the UI
- Charting or graphical visualizations of calibration data
- Modifying `BacktestService` or domain layer logic
- Real-time streaming of individual market evaluations during a run

## Decisions

### 1. Follow existing full-page view pattern exactly

**Decision:** Mirror `TradesManagementView` structure — `FrameView`, `IShortcutHelpProvider`, `Navigator` property, `Escape` to return.

**Rationale:** Consistency reduces cognitive load and ensures architecture tests pass without modification to test patterns. The pattern is proven and well-established.

**Alternatives considered:**
- Dialog-based overlay → rejected; backtests produce substantial data that needs table scrolling.
- Tabbed sub-view inside dashboard → rejected; not enough screen real estate in the 4-panel dashboard layout.

### 2. Singleton BacktestViewModel

**Decision:** Change `BacktestViewModel` DI registration from `Transient` to `Singleton`.

**Rationale:** With transient lifetime, navigating away from `BacktestView` and back would lose results. Singleton ensures results persist for the app lifetime. This matches how `TradesManagementViewModel` and other VMs are registered.

**Alternatives considered:**
- Keep transient but cache results in a separate service → over-engineered; singleton is the established VM pattern.

### 3. Layout: summary panel + results table

**Decision:** Two-region layout within the view:
- **Top region** (~6 rows): `FrameView` with summary metrics displayed as label pairs (Total Markets, Win Rate, Brier Score, Edge Accuracy, Hypothetical P&L, Markets with Edge).
- **Bottom region** (remaining space): `TableView` with columns: Market, Model, Market Price, Edge, Actual, Correct.
- A status label or "Run Backtest" button integrated at the top.

**Rationale:** Matches `BacktestCommand`'s output structure but leverages TUI layout. `TableView` enables scrolling and column alignment for the entries.

### 4. Ctrl+B for navigation shortcut

**Decision:** Register `Ctrl+B` in `MainWindow.OnKeyDown` to show `ActiveView.Backtest`.

**Rationale:** `B` for Backtest is mnemonic. Current shortcuts: `Ctrl+L` (Log), `Ctrl+O` (Trades), `Ctrl+T` (Toggle), `Ctrl+Q` (Quit), `Ctrl+Shift+R` (Reset). No conflict.

### 5. Progress indication during backtest run

**Decision:** Use a simple status label (e.g., "Running backtest..." → "Backtest complete — 25 markets evaluated") that updates via `BacktestViewModel.BacktestCompleted` event. Disable the run button while `IsRunning` is true.

**Rationale:** Simple and consistent with the event-driven MVVM pattern. A full progress bar would require changes to `BacktestService` to report per-market progress, which is out of scope.

## Risks / Trade-offs

- **[Risk] Long-running backtest blocks UI** → Mitigation: `RunBacktestAsync` is already async; the view invokes it on a background task and updates via `App.Invoke()` on completion. The UI remains responsive.
- **[Risk] Singleton VM memory** → Mitigation: `BacktestResult` holds at most ~30 entries — negligible memory. Acceptable trade-off for result persistence.
- **[Risk] Architecture tests may flag new ActiveView member** → Mitigation: `ViewShortcutProviderTests` enumerates `ActiveView` values; adding `Backtest` with a proper `IShortcutHelpProvider` satisfies the test.
