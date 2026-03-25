## 1. ActiveView Enum & ViewNavigator

- [ ] 1.1 Add `Backtest` member to `ActiveView` enum in `src/PolyEdgeScout.Console/App/ActiveView.cs` with XML doc comment matching the existing style.

## 2. ViewModel Lifecycle

- [ ] 2.1 Change `BacktestViewModel` DI registration in `src/PolyEdgeScout.Console/Program.cs` from `AddTransient` to `AddSingleton` so results persist across navigation.

## 3. BacktestView Implementation

- [ ] 3.1 Create `BacktestView` class in `src/PolyEdgeScout.Console/Views/BacktestView.cs` — extend `FrameView`, implement `IShortcutHelpProvider`. Include `Navigator` property for `ViewNavigator`. Set `Title = "Backtest"`, `Visible = false`.
- [ ] 3.2 Add summary panel (top ~6 rows) inside a `FrameView` with labels for: Total Markets, Markets with Edge, Brier Score, Win Rate, Edge Accuracy, Hypothetical P&L. Show dashes when no results exist.
- [ ] 3.3 Add `TableView` (remaining height) with columns: Market, Model, Market Price, Edge, Actual, Correct. Show empty-state row "No backtest results. Press Ctrl+R to run." when no data.
- [ ] 3.4 Add status label (e.g., "Running backtest..." / "Backtest complete — N markets evaluated") between summary and table.
- [ ] 3.5 Handle `Ctrl+R` key in `OnKeyDown` — call `BacktestViewModel.RunBacktestAsync` on a background task if `IsRunning` is false. Update UI via `App.Invoke()` on `BacktestCompleted` event.
- [ ] 3.6 Handle `Escape` key in `OnKeyDown` — call `Navigator?.ShowDashboard()`.
- [ ] 3.7 Implement `GetShortcuts()` returning: `("Escape", "Return to Dashboard")`, `("Ctrl+R", "Run Backtest")`.
- [ ] 3.8 Subscribe to `BacktestViewModel.BacktestCompleted` event to refresh summary labels and rebuild table data.

## 4. Wiring & Integration

- [ ] 4.1 Register `BacktestView` as transient in `src/PolyEdgeScout.Console/Program.cs` DI container (`services.AddTransient<BacktestView>()`).
- [ ] 4.2 Add `BacktestView` parameter to `AppBootstrapper` constructor and pass it through to `MainWindow`.
- [ ] 4.3 Add `BacktestView` parameter to `MainWindow` constructor. Set position/size (X=0, Y=0, Width=Fill, Height=Fill, Visible=false). Call `Navigator.RegisterView(ActiveView.Backtest, backtestView)`. Set `backtestView.Navigator = Navigator`. Add to `MainWindow` children.
- [ ] 4.4 Add `Ctrl+B` handler in `MainWindow.OnKeyDown` — call `Navigator.Show(ActiveView.Backtest)`.
- [ ] 4.5 Add `("Ctrl+B", "Backtest")` to `MainWindow.GetShortcuts()` list.

## 5. Testing

- [ ] 5.1 Verify existing architecture tests pass — `ViewShortcutProviderTests` should detect new `ActiveView.Backtest` and confirm `BacktestView` implements `IShortcutHelpProvider`.
- [ ] 5.2 Run full test suite (`dotnet test`) to confirm no regressions.
