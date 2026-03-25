## Why

The app has two disconnected log displays: a small dashboard panel (`LogPanelView`) showing recent status messages, and a full-page Error Log (`ErrorLogView`) showing only ERR and WRN entries. Users cannot see the complete picture — info, debug, and trade reasoning messages are only visible in the dashboard's limited 50-line summary. When investigating why a trade was placed or skipped, users must cross-reference the small panel with the error log. Replacing the error-only view with a unified full-page log that shows ALL levels gives users a single place to see complete system reasoning.

## What Changes

- **Replace `ErrorLogView`** with a new `FullLogView` that displays ALL log levels (ERR, WRN, INF, DBG) including trade reasoning messages
- **Replace `ErrorLogViewModel`** with a new `FullLogViewModel` that accepts all log levels instead of filtering to ERR/WRN only
- **Rename `ErrorLogEntry`** to `LogEntry` (it now represents any log level, not just errors)
- **Update `ActiveView.ErrorLog`** enum value to `ActiveView.FullLog` to reflect the new purpose
- **Update `ViewNavigator`** — rename `ShowErrorLog()` to `ShowFullLog()` and update all references
- **Update `MainWindow`** — replace `ErrorLogView` with `FullLogView` in constructor, layout, and registration
- **Update `Program.cs`** — wire `FileLogService.LogEntryWritten` to the new `FullLogViewModel.OnLogEntry` (no level filtering)
- **Color-code or prefix log levels** for visual distinction in the full log view (ERR in red, WRN in yellow, INF in default, DBG in dim/gray)
- **Retain all existing UX features** — `Ctrl+L` shortcut, text wrap toggle (`Ctrl+W`), copy to clipboard (`Ctrl+C`, `Ctrl+Shift+C`), `IShortcutHelpProvider` / F1 integration
- **Dashboard `LogPanelView` stays unchanged** as a small summary panel

## Capabilities

### New Capabilities
- `unified-log-display`: Full-page log view showing all log levels (ERR, WRN, INF, DBG) with color-coded level indicators, replacing the error-only view
- `log-level-coloring`: Visual distinction of log levels in the full log view using Terminal.Gui color attributes

### Modified Capabilities

## Impact

- **Views**: `ErrorLogView` removed, `FullLogView` added in `Console/Views/`
- **ViewModels**: `ErrorLogViewModel` removed, `FullLogViewModel` added in `Console/ViewModels/`; `ErrorLogEntry` renamed to `LogEntry`
- **App layer**: `ActiveView` enum updated, `ViewNavigator` method renamed, `MainWindow` constructor updated
- **DI/Wiring**: `Program.cs` updated to register and wire new ViewModel/View
- **Tests**: `ErrorLogViewModel` tests need to be updated or replaced for `FullLogViewModel`; `ViewNavigator` tests updated; architecture tests (`ViewShortcutProviderTests`) updated
- **No domain or infrastructure changes** — `FileLogService` and `ILogService` remain untouched; the `LogEntryWritten` event already provides all levels
