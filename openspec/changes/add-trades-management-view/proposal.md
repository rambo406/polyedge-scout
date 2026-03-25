## Why

The dashboard's "Last 5 Trades" panel only displays the 5 most recent _settled_ trades (via `PnlSnapshot.LastTrades`), but the app has 76 active _open_ trades that are completely invisible. There is no way to view, inspect, or manage open positions — the dashboard panel only shows post-settlement results. Users need a dedicated full-page view to see all open trades, their current status, unrealized P&L, and entry details, plus the ability to review settled trade history.

## What Changes

- **New full-page Trades Management view** — a scrollable `TableView` showing all open trades with columns: market question, action (Buy/Sell), status, entry price, shares, outlay, edge, timestamp. Accessible via `Ctrl+Shift+T` from the dashboard.
- **Settled trades tab/section** — within the same view, a way to toggle or scroll to see settled trade results (the `TradeResult` history), so settled trades are also accessible beyond the dashboard's last-5 limit.
- **ViewNavigator extended** — refactor `ViewNavigator` from a two-state toggle (Dashboard ↔ ErrorLog) to support multiple full-page views (Dashboard, ErrorLog, TradesManagement). Use an enum or view-registry pattern.
- **New ViewModel** — `TradesManagementViewModel` backed by `IOrderService.OpenTrades` and `IOrderService.SettledTrades`, exposing all trade data with no artificial limit.
- **Keyboard shortcut integration** — `Ctrl+Shift+T` opens Trades Management from any view; `Escape` returns to dashboard. The new view implements `IShortcutHelpProvider` so its shortcuts appear in the F1 help overlay.
- **Fix dashboard trades panel data source** — the "Last 5 Trades" panel shows "No trades yet" because `PnlSnapshot.LastTrades` is sourced from `_settledTrades` (settled/resolved trades), not `_openTrades`. This is technically correct behavior (it shows recent results), but the title is misleading when there are 76 open positions. Rename the panel title to "Last 5 Results" or "Recent Settlements" to clarify, and add a hint like "(Ctrl+Shift+T for all trades)".

## Capabilities

### New Capabilities
- `trades-management-view`: Full-page view displaying all open and settled trades with sorting, scrolling, and keyboard navigation
- `multi-view-navigation`: Extend ViewNavigator to support routing between multiple full-page views beyond the current Dashboard ↔ ErrorLog toggle

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Console layer** (`PolyEdgeScout.Console`):
  - New files: `Views/TradesManagementView.cs`, `ViewModels/TradesManagementViewModel.cs`
  - Modified: `App/ViewNavigator.cs` (multi-view support), `Views/MainWindow.cs` (register new view, add shortcut), `Views/TradesView.cs` (rename title), `Program.cs` (DI registration)
- **Application layer** (`PolyEdgeScout.Application`):
  - `IOrderService` already exposes `OpenTrades` and `SettledTrades` properties — no changes needed
- **Domain layer**: No changes
- **Dependencies**: No new NuGet packages — uses existing Terminal.Gui `TableView` widget
- **Tests**: New unit tests for `TradesManagementViewModel`, updated `ViewNavigator` tests for multi-view routing
