## Why

The paper trading system accumulates trades and bankroll changes over time but has no way to start fresh. When testing new strategies, recovering from bugs (e.g., bad weather market trades), or after the bankroll drifts far from a useful testing range, users must restart the entire application — losing their session context — or manually ignore stale data. A dedicated reset feature provides a clean slate without disrupting the running session.

## What Changes

- Add a **paper trading reset** command (keyboard shortcut + menu item) that clears all open trades, settled trades, and resets the bankroll to the configured default value
- Show a **confirmation dialog** before executing the reset to prevent accidental data loss
- **Clear persisted trade data** from the SQLite database (trades, trade results, bankroll state)
- **Log the reset action** with an audit trail entry and user-visible log message
- **Refresh all dashboard views** (portfolio, trades, market table) after reset to reflect the clean state
- The reset command is **only available in Paper mode** — disabled or hidden when in Live mode

## Capabilities

### New Capabilities
- `paper-trading-reset`: Reset paper trading state to initial defaults — clear all open/settled trades, restore bankroll to configured default, clear persisted data, and refresh UI. Only available in Paper mode. Includes confirmation dialog, audit logging, and keyboard shortcut/menu integration.

### Modified Capabilities
<!-- No existing spec-level behavior changes — this is purely additive -->

## Impact

- **`PolyEdgeScout.Application/Interfaces/IOrderService.cs`** — New `ResetPaperTradingAsync()` method on the interface
- **`PolyEdgeScout.Application/Services/OrderService.cs`** — Implementation of reset: clear in-memory state, clear DB records, reset bankroll, audit log entry
- **`PolyEdgeScout.Console/ViewModels/DashboardViewModel.cs`** — New `ResetPaperTrading()` method orchestrating reset + UI refresh
- **`PolyEdgeScout.Console/Views/MainWindow.cs`** — Keyboard shortcut handler (e.g., `Ctrl+R`) for reset
- **`PolyEdgeScout.Console/Views/MenuBarFactory.cs`** — "Reset Paper Trading" menu item under Trading menu
- **`PolyEdgeScout.Console/App/GlobalShortcuts.cs`** — Add `Ctrl+R` to global shortcuts list
- **`PolyEdgeScout.Console/Views/ShortcutHelpDialog`** — Reset shortcut visible in F1 help overlay
- **Tests** — Unit tests for reset logic in OrderService, DashboardViewModel, and architecture tests
