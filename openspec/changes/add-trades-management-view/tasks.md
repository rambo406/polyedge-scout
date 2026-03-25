## 1. Refactor ViewNavigator for Multi-View Support

- [x] 1.1 Create `ActiveView` enum in `App/` with values: `Dashboard`, `ErrorLog`, `TradesManagement`
- [x] 1.2 Refactor `ViewNavigator` to use `ActiveView` enum and a `Dictionary<ActiveView, View>` view registry instead of the boolean `_isErrorLogActive` toggle
- [x] 1.3 Add `Show(ActiveView)` method that hides all views except the target, sets focus, and updates `ActiveProvider`
- [x] 1.4 Keep `ShowDashboard()` and `ShowErrorLog()` as backward-compatible delegates to `Show(ActiveView)`
- [x] 1.5 Add `TradesManagementProvider` property (or generalize provider lookup) so `ActiveProvider` returns the correct `IShortcutHelpProvider` for all three views
- [x] 1.6 Update existing `ViewNavigator` unit tests and add new tests covering Dashboard ↔ TradesManagement and ErrorLog ↔ TradesManagement transitions

## 2. Create TradesManagementViewModel

- [x] 2.1 Create `ViewModels/TradesManagementViewModel.cs` with properties for open trades and settled trades sourced from `IOrderService`
- [x] 2.2 Add `TradesUpdated` event fired when data is refreshed
- [x] 2.3 Add `RefreshTrades()` method that reads from `IOrderService.OpenTrades` and `IOrderService.SettledTrades`
- [x] 2.4 Write unit tests for `TradesManagementViewModel` covering: data refresh, event firing, empty state handling

## 3. Create TradesManagementView

- [x] 3.1 Create `Views/TradesManagementView.cs` as a `FrameView` implementing `IShortcutHelpProvider`
- [x] 3.2 Add a `TabView` with two tabs: "Open Trades" and "Settled Trades"
- [x] 3.3 Implement "Open Trades" tab with a `TableView` displaying columns: Market Question (truncated to 40 chars), Action, Status, Entry Price, Shares, Outlay, Edge, Timestamp
- [x] 3.4 Implement "Settled Trades" tab with a `TableView` displaying columns: Market Question, Won/Lost, Entry Price, Shares, Net Profit, ROI, Settled At
- [x] 3.5 Handle `Escape` key to navigate back to dashboard via `ViewNavigator`
- [x] 3.6 Implement `IShortcutHelpProvider.GetShortcuts()` returning view-specific shortcuts
- [x] 3.7 Subscribe to `TradesManagementViewModel.TradesUpdated` to refresh table data
- [x] 3.8 Handle empty state — show "No open trades" / "No settled trades" messages when lists are empty

## 4. Wire Up in MainWindow and DI

- [x] 4.1 Register `TradesManagementViewModel` as singleton in `Program.cs` DI container
- [x] 4.2 Register `TradesManagementView` in `Program.cs` DI container
- [x] 4.3 Update `MainWindow` constructor to accept `TradesManagementView` parameter
- [x] 4.4 Position `TradesManagementView` as full-page (X=0, Y=0, Width=Fill, Height=Fill, Visible=false) and add to `MainWindow`
- [x] 4.5 Pass `TradesManagementView` to `ViewNavigator` during construction (update view registry)
- [x] 4.6 Add `Ctrl+Shift+T` keyboard handler in `MainWindow.OnKeyDown` to call `Navigator.Show(ActiveView.TradesManagement)`
- [x] 4.7 Update `MainWindow.GetShortcuts()` to include `Ctrl+Shift+T` → "All Trades"

## 5. Integrate Data Flow

- [x] 5.1 Update `DashboardViewModel.RunScanLoopAsync` to also call `TradesManagementViewModel.RefreshTrades()` after each scan cycle
- [x] 5.2 Verify `ErrorLogView` `Escape` handler and `Ctrl+Shift+T` handler work correctly with refactored `ViewNavigator`

## 6. Fix Dashboard Trades Panel

- [x] 6.1 Rename `TradesView` title from "Last 5 Trades" to "Recent Settlements"
- [x] 6.2 Add hint line "(Ctrl+Shift+T for all trades)" to the dashboard trades panel when displaying "No trades yet" or after the trades list

## 7. Verification

- [x] 7.1 Run all existing architecture tests to confirm no regressions
- [x] 7.2 Run all existing unit tests to confirm no regressions
- [ ] 7.3 Manual smoke test: launch app, verify dashboard shows "Recent Settlements" panel, press `Ctrl+Shift+T` to open trades view, verify all open trades are visible, press `Escape` to return, press `F1` to verify help shows updated shortcuts
