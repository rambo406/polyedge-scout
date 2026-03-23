## 1. Fix Console Output Bleed-through

- [ ] 1.1 Remove `Console.WriteLine(line)` from `FileLogService.Write` method
- [ ] 1.2 Add `LogEntryWritten` event (`Action<string, string>` — level, formatted line) to `FileLogService`
- [ ] 1.3 Raise `LogEntryWritten` in `FileLogService.Write` after writing to file and buffer
- [ ] 1.4 Write unit tests verifying `FileLogService` does not call `Console.WriteLine` and fires the event

## 2. Error Log ViewModel

- [ ] 2.1 Create `ErrorLogEntry` record in `Console/ViewModels/` with properties: `DateTime Timestamp`, `string Level`, `string Message`
- [ ] 2.2 Create `ErrorLogViewModel` in `Console/ViewModels/` with bounded `List<ErrorLogEntry>` (max 500), `IReadOnlyList<ErrorLogEntry> Entries`, and `EntryAdded` event
- [ ] 2.3 Add `OnLogEntry(string level, string formattedLine)` method to `ErrorLogViewModel` that filters for `ERR`/`WRN` levels and adds entries
- [ ] 2.4 Write unit tests for `ErrorLogViewModel`: entry filtering, buffer bounding, event firing

## 3. Error Indicator on Dashboard

- [ ] 3.1 Add `LastError` property and `ErrorChanged` event to `DashboardViewModel`
- [ ] 3.2 Set `LastError` in the scan loop's `catch` block; clear it on successful scan completion
- [ ] 3.3 Create `ErrorIndicatorView` in `Console/Views/` — a `FrameView` or `Label` with red/yellow `ColorScheme`, bound to `DashboardViewModel.ErrorChanged`
- [ ] 3.4 Add `ErrorIndicatorView` to `MainWindow` layout at `Y=0`, push other content down by 1 row when visible, hide when `LastError` is null
- [ ] 3.5 Write unit tests for `DashboardViewModel.LastError` lifecycle (set on error, clear on success)

## 4. Error Log View (Full-Screen)

- [ ] 4.1 Create `ErrorLogView` in `Console/Views/` — a `FrameView` with a `ListView` bound to `ErrorLogViewModel.Entries`, showing timestamp + level + message per row
- [ ] 4.2 Subscribe `ErrorLogView` to `ErrorLogViewModel.EntryAdded` to refresh the list and auto-scroll to bottom
- [ ] 4.3 Write unit tests for `ErrorLogView` data binding (verify list updates on `EntryAdded`)

## 5. View Navigation

- [ ] 5.1 Create `ViewNavigator` class in `Console/App/` with `ShowDashboard()` and `ShowErrorLog()` methods that toggle `Visible` on dashboard content vs error log content
- [ ] 5.2 Modify `MainWindow` to accept both dashboard content views and `ErrorLogView`; initially show dashboard, hide error log
- [ ] 5.3 Add `Ctrl+L` keybinding to `MenuBarFactory` — calls `ViewNavigator.ShowErrorLog()`
- [ ] 5.4 Add `Escape` key handler in `ErrorLogView` — calls `ViewNavigator.ShowDashboard()`
- [ ] 5.5 Add "Error Log" menu item with `Ctrl+L` accelerator under the "View" menu in `MenuBarFactory`

## 6. DI Wiring & Integration

- [ ] 6.1 Register `ErrorLogViewModel` as transient in `Program.cs`
- [ ] 6.2 Register `ErrorLogView` as transient in `Program.cs`
- [ ] 6.3 Register `ViewNavigator` as transient in `Program.cs`
- [ ] 6.4 Wire `FileLogService.LogEntryWritten` event to `ErrorLogViewModel.OnLogEntry` in `Program.cs` (cast `ILogService` to `FileLogService`)
- [ ] 6.5 Update `AppBootstrapper` constructor to accept `ErrorLogView` and `ViewNavigator`; pass them to `MainWindow`
- [ ] 6.6 Smoke-test the full application: verify no flashing banner, error indicator shows on API failure, Ctrl+L opens Error Log view, Escape returns to dashboard
