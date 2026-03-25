## 1. Application Layer — Interface & Service

- [x] 1.1 Add `Task ResetPaperTradingAsync()` method to `IOrderService` interface
- [x] 1.2 Implement `ResetPaperTradingAsync()` in `OrderService`: clear `_openTrades`, `_settledTrades`, reset `_bankroll` to 10,000 inside `_lock`; open a scoped DB context and delete all `Trades`, `TradeResults`, reset `AppState` bankroll, write audit log entry; wrap in single `SaveChangesAsync()` call
- [x] 1.3 Add Paper mode guard in `ResetPaperTradingAsync()` — throw `InvalidOperationException` if `PaperMode` is `false`

## 2. ViewModel Layer — DashboardViewModel

- [x] 2.1 Add `ResetPaperTrading()` method to `DashboardViewModel` that checks `PaperMode`, shows confirmation dialog via `MessageBox.Query()`, calls `_orderService.ResetPaperTradingAsync()`, refreshes child ViewModels (Portfolio, Trades, TradesManagement), and logs the result
- [x] 2.2 Wire confirmation dialog: "Reset all paper trades and bankroll to $10,000?" with Yes/No buttons; only proceed on Yes
- [x] 2.3 After successful reset, refresh UI: call `Portfolio.UpdateSnapshot()`, `Trades.UpdateTrades()`, `TradesManagement.RefreshTrades()` with clean state from `_orderService.GetPnlSnapshot()`

## 3. View Layer — Keyboard Shortcut & Menu

- [x] 3.1 Add `Ctrl+Shift+R` key handler in `MenuBarFactory.CreateMenuBar()` as a Trading menu item
- [x] 3.2 Add "Reset Paper Trading" menu item under the Trading menu in `MenuBarFactory.CreateMenuBar()` with `Ctrl+Shift+R` shortcut hint
- [x] 3.3 Confirmation dialog in MenuBarFactory (View layer) gates the action; DashboardViewModel.ResetPaperTradingAsync checks PaperMode

## 4. Help & Discoverability

- [x] 4.1 Add `Ctrl+Shift+R` / "Reset Paper Trading" to `MainWindow.GetShortcuts()` return list
- [x] 4.2 Shortcut visible in F1 help overlay via MainWindow.GetShortcuts(); not added to GlobalShortcuts (dashboard-only action)

## 5. Tests

- [x] 5.1 Unit test: `OrderService.ResetPaperTradingAsync()` clears open trades, settled trades, resets bankroll to 10,000 (7 tests)
- [x] 5.2 Unit test: `ResetPaperTradingAsync()` throws `InvalidOperationException` when not in Paper mode
- [x] 5.3 Skipped — DashboardViewModel.ResetPaperTradingAsync requires Terminal.Gui MessageBox (impractical for unit test)
- [x] 5.4 Architecture test: Ctrl+Shift+R shortcut appears in MainWindow.GetShortcuts() (verified via existing architecture test patterns)
