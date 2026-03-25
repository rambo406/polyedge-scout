## Context

PolyEdge Scout is a .NET Clean Architecture console app using Terminal.Gui v2. The dashboard currently has five child views: `MarketTableView`, `PortfolioView`, `TradesView` ("Last 5 Trades"), `LogPanelView`, and `ErrorIndicatorView`. A full-page `ErrorLogView` is accessible via `Ctrl+L`, managed by a `ViewNavigator` that toggles between two states: Dashboard and ErrorLog.

The `IOrderService` maintains two in-memory collections: `_openTrades` (active `Trade` entities) and `_settledTrades` (resolved `TradeResult` entities). The dashboard trades panel is fed from `PnlSnapshot.LastTrades`, which sources from `_settledTrades` only — so 76 open trades are completely invisible.

The app follows an MVVM pattern: ViewModels are registered as singletons in DI, views take ViewModels as constructor parameters, and keyboard shortcuts are handled via `OnKeyDown` overrides with `IShortcutHelpProvider` for F1 help discoverability.

## Goals / Non-Goals

**Goals:**
- Provide a full-page view showing all open trades and settled trade history
- Extend the view navigation system to support multiple full-page views cleanly
- Clarify the dashboard "Last 5 Trades" panel title to avoid user confusion
- Follow existing patterns (MVVM, DI, `IShortcutHelpProvider`, `ViewNavigator`)
- Make the new view accessible via a dedicated keyboard shortcut

**Non-Goals:**
- Trade execution or modification from the trades view (read-only for now)
- Real-time price streaming or live P&L updates within the view (deferred to future work)
- Filtering or search within the trades list (could be added later)
- Changing the `IOrderService` interface or domain entities
- Mobile or web UI — this is Terminal.Gui only

## Decisions

### 1. ViewNavigator: enum-based state machine vs. view registry

**Decision**: Refactor `ViewNavigator` to use an enum `ActiveView { Dashboard, ErrorLog, TradesManagement }` with a `Show(ActiveView)` method.

**Rationale**: The current two-state boolean (`_isErrorLogActive`) doesn't scale. An enum is minimal, type-safe, and avoids building a full view-registry framework for just 3 views. A dictionary-based registry (`Dictionary<ActiveView, View>`) maps each enum value to its view instance.

**Alternative considered**: Generic `Show<T>()` with runtime type lookup — rejected because it adds unnecessary complexity and loses compile-time safety for 3 known views.

### 2. Keyboard shortcut: `Ctrl+Shift+T`

**Decision**: Use `Ctrl+Shift+T` to open Trades Management.

**Rationale**: `Ctrl+T` is already used for "Toggle Mode" (paper/live). `Ctrl+Shift+T` is ergonomically close and follows the Ctrl+modifier pattern. `Escape` returns to the dashboard (consistent with ErrorLogView).

### 3. Trade data: use both `OpenTrades` and `SettledTrades` from `IOrderService`

**Decision**: The `TradesManagementViewModel` reads directly from `IOrderService.OpenTrades` and `IOrderService.SettledTrades` properties, refreshed on each scan cycle via event subscription.

**Rationale**: These properties already exist and are thread-safe (lock-guarded). No new Application/Domain layer APIs needed. The ViewModel transforms data into display rows.

**Alternative considered**: Adding a new `GetAllTrades()` method on `IOrderService` — rejected because it would duplicate existing surface area unnecessarily.

### 4. View layout: `TableView` with tabbed sections

**Decision**: Use Terminal.Gui `TableView` for displaying trade data in a columnar format. Use a `TabView` with two tabs: "Open Trades" and "Settled Trades".

**Rationale**: `TableView` provides built-in column headers, scrolling, and keyboard navigation — ideal for tabular trade data. `TabView` cleanly separates the two data sources without needing a custom toggle mechanism. Both are standard Terminal.Gui widgets.

**Alternative considered**: Single `ListView` with formatted strings (like the current `TradesView`) — rejected because columnar alignment is important for trade data readability, and `TableView` handles this natively.

### 5. Dashboard panel: rename, don't remove

**Decision**: Rename the dashboard `TradesView` title from "Last 5 Trades" to "Recent Settlements" and add a hint line "(Ctrl+Shift+T for all trades)".

**Rationale**: The panel correctly shows settled trades (results with P&L). Removing it would lose at-a-glance info. Renaming clarifies what it shows and points users to the full view.

## Risks / Trade-offs

- **[Risk] ViewNavigator refactor breaks existing ErrorLog navigation** → Mitigation: Existing architecture tests for `ViewNavigator` cover the Dashboard ↔ ErrorLog toggle; extend those tests to cover the new state before modifying production code.
- **[Risk] `TableView` column width with long market questions** → Mitigation: Truncate market question column to a max width with ellipsis, consistent with the existing `TradesView` approach.
- **[Risk] Performance with large trade counts** → Mitigation: Terminal.Gui `TableView` uses virtual rendering (only visible rows are drawn). 76 trades is well within performance limits. For future scaling, consider pagination.
- **[Trade-off] Read-only view initially** → Keeps scope small and avoids risky trade modification logic. Can add actions (cancel, close) in a follow-up change.
