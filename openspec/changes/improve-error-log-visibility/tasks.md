## 1. Fix Console Output Bleed-through

- [x] 1.1 Remove `Console.WriteLine(line)` from `FileLogService.Write` method
- [x] 1.2 Add `LogEntryWritten` event (`Action<string, string>` — level, formatted line) to `FileLogService`
- [x] 1.3 Raise `LogEntryWritten` in `FileLogService.Write` after writing to file and buffer
- [x] 1.4 Write unit tests verifying `FileLogService` does not call `Console.WriteLine` and fires the event

## 2. Error Log ViewModel

- [x] 2.1 Create `ErrorLogEntry` record in `Console/ViewModels/` with properties: `DateTime Timestamp`, `string Level`, `string Message`
- [x] 2.2 Create `ErrorLogViewModel` in `Console/ViewModels/` with bounded `List<ErrorLogEntry>` (max 500), `IReadOnlyList<ErrorLogEntry> Entries`, and `EntryAdded` event
- [x] 2.3 Add `OnLogEntry(string level, string formattedLine)` method to `ErrorLogViewModel` that filters for `ERR`/`WRN` levels and adds entries
- [x] 2.4 Write unit tests for `ErrorLogViewModel`: entry filtering, buffer bounding, event firing

## 3. Error Indicator on Dashboard

- [x] 3.1 Add `LastError` property and `ErrorChanged` event to `DashboardViewModel`
- [x] 3.2 Set `LastError` in the scan loop's `catch` block; clear it on successful scan completion
- [x] 3.3 Create `ErrorIndicatorView` in `Console/Views/` — a `Label` with red/white `ColorScheme`, bound to `DashboardViewModel.ErrorChanged`
- [x] 3.4 Add `ErrorIndicatorView` to `MainWindow` layout at `Y=0`, push other content down by 1 row when visible, hide when `LastError` is null
- [x] 3.5 Write unit tests for `DashboardViewModel.LastError` lifecycle (set on error, clear on success)

## 4. Error Log View (Full-Screen)

- [x] 4.1 Create `ErrorLogView` in `Console/Views/` — a `FrameView` with a `ListView` bound to `ErrorLogViewModel.Entries`, showing formatted message per row
- [x] 4.2 Subscribe `ErrorLogView` to `ErrorLogViewModel.EntryAdded` to refresh the list and auto-scroll to bottom
- [x] 4.3 Skipped — Terminal.Gui views require Application.Init() which is impractical for unit tests; ViewModel binding tested in Group 2

## 5. View Navigation

- [x] 5.1 Create `ViewNavigator` class in `Console/App/` with `ShowDashboard()` and `ShowErrorLog()` methods that toggle `Visible` on dashboard content vs error log content
- [x] 5.2 Modify `MainWindow` to accept both dashboard content views and `ErrorLogView`; initially show dashboard, hide error log
- [x] 5.3 Add `Ctrl+L` keybinding to `MenuBarFactory` — calls `ViewNavigator.ShowErrorLog()`
- [x] 5.4 Add `Escape` key handler in `ErrorLogView` — calls `ViewNavigator.ShowDashboard()`
- [x] 5.5 Add "Error Log" menu item with `Ctrl+L` accelerator under the "View" menu in `MenuBarFactory`

## 6. DI Wiring & Integration

- [x] 6.1 Register `ErrorLogViewModel` as singleton in `Program.cs` (singleton needed for shared event wiring)
- [x] 6.2 Register `ErrorLogView` and `ErrorIndicatorView` as transient in `Program.cs`
- [x] 6.3 ViewNavigator created inside MainWindow — no DI registration needed
- [x] 6.4 Wire `FileLogService.LogEntryWritten` event to `ErrorLogViewModel.OnLogEntry` in `Program.cs` (cast `ILogService` to `FileLogService`)
- [x] 6.5 Update `AppBootstrapper` constructor to accept `ErrorIndicatorView` and `ErrorLogView`; pass them to `MainWindow`; pass `Navigator` to `MenuBarFactory`
- [ ] 6.6 Smoke-test the full application: verify no flashing banner, error indicator shows on API failure, Ctrl+L opens Error Log view, Escape returns to dashboard
