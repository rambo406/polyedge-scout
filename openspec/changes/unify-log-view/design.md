## Context

The application currently has two separate log display mechanisms:

1. **`LogPanelView`** — a small dashboard panel that shows the last 50 messages from `LogViewModel`, which syncs from `ILogService.RecentMessages` (a 100-entry circular buffer in `FileLogService`). This shows all levels but in a compact, space-constrained view.

2. **`ErrorLogView`** — a full-page view (toggled via `Ctrl+L`) backed by `ErrorLogViewModel`, which subscribes to `FileLogService.LogEntryWritten` but filters to only ERR and WRN levels. It supports word wrap toggle, copy to clipboard, and shortcut help.

The `FileLogService.LogEntryWritten` event already fires for ALL log levels with `(string level, string formattedLine)`. The filtering happens in `ErrorLogViewModel.OnLogEntry`, which discards anything that isn't ERR or WRN. This means the infrastructure is already capable of delivering all log levels — only the ViewModel layer needs to change.

The `ViewNavigator` manages switching between Dashboard, ErrorLog, and TradesManagement views. The `ActiveView` enum identifies each view. `MainWindow` constructs all views and registers them with the navigator.

## Goals / Non-Goals

**Goals:**
- Provide a single full-page view showing ALL log output (ERR, WRN, INF, DBG) so users can trace complete system reasoning
- Color-code log levels for quick visual scanning (ERR red, WRN yellow, INF default, DBG gray)
- Retain all existing UX features from ErrorLogView: word wrap, copy, shortcut help, Esc to return
- Keep the dashboard `LogPanelView` unchanged as a quick-glance summary

**Non-Goals:**
- Log level filtering/toggling in the UI (can be added later but not in this change)
- Searching or text filtering within the log view
- Changing `ILogService` domain interface or `FileLogService` infrastructure
- Modifying how `LogPanelView` or `LogViewModel` work

## Decisions

### 1. Replace `ErrorLogViewModel` with `FullLogViewModel`

**Decision**: Create a new `FullLogViewModel` that replaces `ErrorLogViewModel`. The new ViewModel removes the level filter in `OnLogEntry` — all levels are accepted. The `ErrorLogEntry` record is renamed to `LogEntry` since it now represents any log level.

**Rationale**: The existing `ErrorLogViewModel` is purpose-built to filter for ERR/WRN. Rather than adding a flag to toggle filtering, a clean replacement is simpler and avoids dead code paths. The API surface stays identical (bounded buffer, `OnLogEntry`, `ToggleWordWrap`, `GetSelectedEntryText`, `GetAllEntriesText`).

**Alternative considered**: Add an `AcceptAllLevels` property to `ErrorLogViewModel`. Rejected because the name becomes misleading and the class has a single clear purpose.

### 2. Replace `ErrorLogView` with `FullLogView`

**Decision**: Create `FullLogView` with the same structure as `ErrorLogView` (ListView + TextView for wrap mode). Add color-coded log level rendering using Terminal.Gui `ColorScheme` or `Attribute` on list items. Title changes from "Error Log" to "Log".

**Rationale**: The view structure is proven and works well. The only additions are color coding and the title change.

### 3. Color-code log levels using `ColorScheme` attributes

**Decision**: In `FullLogView`, when rendering list items, apply Terminal.Gui color attributes based on the log level prefix in each message:
- `[ERR]` → Red foreground
- `[WRN]` → Yellow foreground
- `[INF]` → Default foreground
- `[DBG]` → Gray/dim foreground

Implementation: Use a custom `IListDataSource` that provides per-row color rendering, or apply colors when building the display string with Terminal.Gui's `Attribute` API.

**Alternative considered**: Simply prefix levels with emoji or ASCII markers (⛔, ⚠️). Rejected because Terminal.Gui v2 supports color attributes natively and color provides better scannability.

### 4. Rename `ActiveView.ErrorLog` to `ActiveView.FullLog`

**Decision**: Rename the enum value and update `ViewNavigator.ShowErrorLog()` to `ShowFullLog()`. All references throughout the codebase (MainWindow, tests) are updated.

**Rationale**: The enum should describe what the view IS, not what it was. A rename is safe since it's a terminal UI app with no external API contracts.

### 5. Wire `FullLogViewModel` in `Program.cs`

**Decision**: In `Program.cs`, replace the `ErrorLogViewModel` registration with `FullLogViewModel`. The `FileLogService.LogEntryWritten += fullLogVm.OnLogEntry` wiring stays identical — the only difference is `OnLogEntry` no longer filters.

**Rationale**: Minimal change. The event signature is unchanged.

### 6. Update `ErrorIndicatorView` behavior

**Decision**: Keep `ErrorIndicatorView` as-is. It currently tracks error counts for the dashboard indicator. This is independent of the log view unification — the indicator still needs to know about errors specifically, and it can continue using its own counting logic or subscribe separately.

**Rationale**: The error indicator serves a different purpose (alerting users to errors) and doesn't need to change just because the full log view now shows all levels.

## Risks / Trade-offs

- **[Performance with high log volume]** → The 500-entry bounded buffer in FullLogViewModel caps memory. Terminal.Gui ListView handles hundreds of items efficiently. No mitigation needed unless profiling shows issues.
- **[Color rendering in non-color terminals]** → Terminal.Gui gracefully degrades on terminals without color support. Colors are additive, not required for readability (level prefixes like `[ERR]` remain in the text).
- **[Lost error-only view]** → Users who preferred seeing only errors lose that focused view. Mitigation: A future change can add level filtering with checkboxes or a filter shortcut. For now, color coding and the `[ERR]` prefix make errors easy to spot in the unified view.
- **[Test updates]** → Existing tests for `ErrorLogViewModel` need to be rewritten for `FullLogViewModel`. The test surface is small (filtering, buffer bounds, word wrap toggle).
