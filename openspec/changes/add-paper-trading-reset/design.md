## Context

PolyEdgeScout is a .NET Clean Architecture TUI application using Terminal.Gui v2 for paper/live crypto prediction market trading. The paper trading system maintains state in two places:

1. **In-memory** — `OrderService` holds `_openTrades`, `_settledTrades`, and `_bankroll` behind a `lock`
2. **SQLite database** — `TradingDbContext` persists `Trades`, `TradeResults`, `AuditLog`, and `AppState` via EF Core

On startup, `OrderService.InitializeAsync()` recovers state from the database. The default bankroll is hardcoded at `$10,000` in `OrderService` and matches the `appsettings.json` configuration. Currently, there is no way to reset to a clean state without restarting the application and manually clearing the database.

The application uses a keyboard-driven UI with global shortcuts (`Ctrl+T` for mode toggle, `F5` for refresh, `Ctrl+L` for full log, `Ctrl+O` for trades management) and a menu bar created by `MenuBarFactory`. `DashboardViewModel` orchestrates state and raises events to update UI views.

## Goals / Non-Goals

**Goals:**
- Provide a single-action paper trading reset accessible via keyboard shortcut and menu
- Clear all in-memory and persisted trade data atomically
- Restore bankroll to the configured default value ($10,000)
- Prevent accidental resets with a confirmation dialog
- Log the reset action for audit trail compliance (consistent with existing audit patterns)
- Refresh all dashboard views immediately after reset
- Gate the feature to Paper mode only

**Non-Goals:**
- Partial reset (e.g., clear only settled trades, keep open trades) — full reset only
- Undo/rollback of the reset action
- Reset of scan history, log messages, or market data
- Configurable reset bankroll (uses the same hardcoded $10,000 as `OrderService`)
- Live mode reset or any live trading state manipulation

## Decisions

### 1. Add `ResetPaperTradingAsync()` to `IOrderService`

**Choice**: Extend the existing `IOrderService` interface with a new async method.

**Rationale**: The reset is fundamentally an order service operation — it modifies trades and bankroll, which are already `IOrderService`'s responsibility. The method handles both in-memory state clearing and database cleanup in a single transaction.

**Alternatives considered**:
- *Separate `IResetService`* — Over-engineered for a single method; would add unnecessary DI complexity.
- *Static method on `OrderService`* — Violates the interface abstraction and makes testing harder.

### 2. Keyboard shortcut: `Ctrl+Shift+R`

**Choice**: Use `Ctrl+Shift+R` rather than `Ctrl+R`.

**Rationale**: `Ctrl+R` is commonly associated with "refresh" in many applications, which could cause confusion with the existing `F5` refresh. The Shift modifier adds a deliberate friction layer that communicates "this is a destructive action," similar to how `Ctrl+Shift+Delete` is used for clearing browser data. This pairs with the confirmation dialog for double protection.

**Alternatives considered**:
- *`Ctrl+R`* — Too easily confused with refresh; accidentally hitting it could trigger an unwanted confirmation dialog.
- *Menu-only (no shortcut)* — Inconsistent with the app's keyboard-first UX paradigm.

### 3. Confirmation dialog via Terminal.Gui `MessageBox`

**Choice**: Use `MessageBox.Query()` with Yes/No buttons before executing the reset.

**Rationale**: Terminal.Gui's built-in `MessageBox` provides a modal dialog that blocks interaction until dismissed — perfect for a destructive confirmation. It's already used elsewhere in the app's patterns and requires no custom view code.

### 4. Database cleanup in a single EF Core transaction

**Choice**: Use a scoped `IServiceScopeFactory` (matching existing `OrderService` patterns) to open a scope, delete all `Trades` and `TradeResults`, reset the `AppState` bankroll, write an audit log entry, and call `SaveChangesAsync()` once.

**Rationale**: Atomic operation ensures consistency — we either fully reset or not at all. This matches the pattern already established in `OrderService.InitializeAsync()` and `ExecutePaperTradeAsync()` for scoped DB access.

### 5. DashboardViewModel orchestrates the reset flow

**Choice**: `DashboardViewModel` calls the confirmation dialog, invokes `IOrderService.ResetPaperTradingAsync()`, then refreshes child ViewModels.

**Rationale**: The ViewModel already owns the `ToggleMode()` and `RequestRefresh()` patterns, and has references to all child ViewModels. This keeps the View layer thin (just forwarding the key event) and the service layer unaware of UI concerns.

## Risks / Trade-offs

- **[Concurrent scan during reset]** → The reset modifies `_openTrades`/`_settledTrades` behind the existing `_lock`. If a scan cycle is mid-flight, the lock ensures atomic access. However, a scan result arriving *just after* reset could immediately create new trades. **Mitigation**: This is acceptable — the user expects new scans to continue trading from the clean state.

- **[Audit log entries reference deleted trades]** → Existing audit entries for settled/created trades will reference trade IDs that no longer exist in the `Trades` table. **Mitigation**: Audit log is append-only by design; orphaned references are expected and documented in the reset audit entry itself.

- **[No undo]** → Reset is permanent. **Mitigation**: Confirmation dialog + Shift modifier on shortcut provide two layers of protection. The audit log records what was cleared.
