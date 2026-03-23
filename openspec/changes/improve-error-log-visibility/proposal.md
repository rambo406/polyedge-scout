## Why

Error messages from API failures and scan cycle errors appear as a transient banner that flashes on-screen for a fraction of a second before the next TUI render wipes it. This happens because `FileLogService.Write` calls `Console.WriteLine` directly, which bleeds raw text onto the Terminal.Gui surface and is immediately overwritten. Users cannot read the error, cannot review past errors, and have no dedicated place to inspect what went wrong. The existing Log panel at the bottom of the dashboard is a small, mixed-purpose feed limited to 50 visible lines — it intermingles informational messages with errors, making triage difficult.

## What Changes

- **Remove raw `Console.WriteLine` from `FileLogService`** so that log output no longer bleeds through the Terminal.Gui surface as a flashing banner. All log messages will be routed exclusively through the TUI's own views.
- **Add a persistent error indicator to the dashboard** — a small, always-visible label/bar that shows the most recent error message and stays on-screen until the next scan succeeds or the user dismisses it. This replaces the accidental flash banner with intentional, readable feedback.
- **Add a dedicated Error Log view** — a full-page view (accessible via `Ctrl+L` or a "View → Error Log" menu item) that shows only `ERR` and `WRN` level messages in a scrollable, filterable list. Users can navigate there, review all errors, and return to the dashboard.
- **Introduce page/tab navigation** to the app so users can switch between the Dashboard view and the Error Log view (and future views). The status bar and menu bar remain visible across views.
- **Surface `ILogService` errors into `LogViewModel`** — errors logged anywhere in the stack (API clients, services) will be captured and forwarded to the error log view model, not just errors caught in the scan loop.

## Capabilities

### New Capabilities
- `error-indicator`: Persistent error banner on the dashboard that shows the last error and remains visible until cleared
- `error-log-view`: Full-page scrollable view showing filtered ERR/WRN messages with timestamps, accessible via keyboard shortcut
- `view-navigation`: Tab/page switching infrastructure to move between Dashboard and Error Log views while preserving shared chrome (menu bar, status bar)

### Modified Capabilities
<!-- No existing specs to modify — openspec/specs/ is empty -->

## Impact

- **PolyEdgeScout.Infrastructure** — `FileLogService.Write` will stop calling `Console.WriteLine`; add an event or callback mechanism so the Console layer can subscribe to new log entries
- **PolyEdgeScout.Console/ViewModels** — New `ErrorLogViewModel`; extend `LogViewModel` or `DashboardViewModel` to track latest error for the indicator
- **PolyEdgeScout.Console/Views** — New `ErrorLogView` (full-page), new `ErrorIndicatorView` (dashboard widget); modify `MainWindow` and `AppBootstrapper` to support view switching
- **PolyEdgeScout.Console/Views/MenuBarFactory** — Add `Ctrl+L` shortcut and "View → Error Log" menu item
- **PolyEdgeScout.Console/Program.cs** — Register new ViewModels and Views in DI
- **No breaking changes** — all changes are additive to the Console layer; Domain and Application layers are unaffected
