## Why

The app already has a `BacktestService` and CLI `BacktestCommand` for running backtests, but there is no interactive TUI page for executing and viewing backtests. Users must exit the TUI, run a separate CLI command, and lose context. A dedicated full-page BacktestView inside the TUI would let users run backtests, inspect results, and compare runs without leaving the application — matching the established full-page view pattern (FullLogView, TradesManagementView).

## What Changes

- Add `ActiveView.Backtest` to the view enum so `ViewNavigator` can manage the new page.
- Create `BacktestView` — a full-page `FrameView` displaying backtest summary metrics, a `TableView` of individual market entries, and calibration data.
- Provide a "Run Backtest" button/shortcut (`Ctrl+R` within the view) that invokes `BacktestViewModel.RunBacktestAsync` with a progress indicator.
- Register `Ctrl+B` in `MainWindow` to navigate to the BacktestView.
- Wire `BacktestView` into `AppBootstrapper`, `MainWindow` constructor, `Program.cs` DI, and `ViewNavigator`.
- Upgrade `BacktestViewModel` registration from `Transient` to `Singleton` so it retains the last run's results across navigation.
- Implement `IShortcutHelpProvider` on `BacktestView` for F1 help overlay integration.

## Capabilities

### New Capabilities
- `backtest-view`: Interactive full-page TUI view for running backtests, displaying summary metrics, market entry table, and calibration data with keyboard navigation.

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Console layer**: New `BacktestView` class; changes to `MainWindow`, `AppBootstrapper`, `Program.cs`, `ActiveView` enum.
- **ViewModel layer**: `BacktestViewModel` lifetime changes from transient to singleton.
- **Architecture tests**: `ViewShortcutProviderTests` may need updating for the new view; `ActiveView` enum references in tests.
- **No domain/application/infrastructure changes** — the existing `BacktestService` and DTOs are reused as-is.
