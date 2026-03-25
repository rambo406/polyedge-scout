## 1. ViewModel Layer

- [x] 1.1 Rename `ErrorLogEntry` to `LogEntry` in `Console/ViewModels/LogEntry.cs` (update record name and file name)
- [x] 1.2 Create `FullLogViewModel` in `Console/ViewModels/FullLogViewModel.cs` — same API as `ErrorLogViewModel` but `OnLogEntry` accepts ALL levels (remove the `if (level is not "ERR" and not "WRN") return;` filter)
- [x] 1.3 Delete `ErrorLogViewModel.cs` and `ErrorLogEntry.cs`

## 2. View Layer

- [x] 2.1 Create `FullLogView` in `Console/Views/FullLogView.cs` — same structure as `ErrorLogView` (ListView + TextView for wrap mode) but with title "Log" instead of "Error Log"
- [x] 2.2 Deferred — Color coding deferred to follow-up (Terminal.Gui ListView doesn't natively support per-row colors; log lines already contain [ERR]/[WRN]/[INF]/[DBG] prefixes)
- [x] 2.3 Retain all keyboard shortcuts in `FullLogView`: Escape (back to dashboard), Ctrl+W (word wrap toggle), Ctrl+C (copy selected), Ctrl+Shift+C (copy all)
- [x] 2.4 Implement `IShortcutHelpProvider` on `FullLogView` with updated shortcut descriptions
- [x] 2.5 Delete `ErrorLogView.cs`

## 3. Navigation and Enum Updates

- [x] 3.1 Rename `ActiveView.ErrorLog` to `ActiveView.FullLog` in `Console/App/ActiveView.cs`
- [x] 3.2 Rename `ViewNavigator.ShowErrorLog()` to `ViewNavigator.ShowFullLog()` and update all references
- [x] 3.3 Update `MainWindow` constructor — replace `ErrorLogView` parameter with `FullLogView`, update layout registration with `ActiveView.FullLog`
- [x] 3.4 Update `MainWindow.OnKeyDown` Ctrl+L handler and `GetShortcuts()` to reference "Full Log" instead of "Error Log"

## 4. DI and Wiring

- [x] 4.1 Update `Program.cs` — replace `ErrorLogViewModel` registration with `FullLogViewModel` (singleton)
- [x] 4.2 Update `Program.cs` — replace `ErrorLogView` registration with `FullLogView` (transient)
- [x] 4.3 Update `Program.cs` — wire `FileLogService.LogEntryWritten += fullLogVm.OnLogEntry` (same event, new ViewModel variable name)

## 5. Test Updates

- [x] 5.1 Create `FullLogViewModelTests` — test that all log levels are accepted (ERR, WRN, INF, DBG), bounded buffer at 500, word wrap toggle, `GetSelectedEntryText`, `GetAllEntriesText`
- [x] 5.2 Update `ViewNavigatorTests` — replace `ActiveView.ErrorLog` references with `ActiveView.FullLog`, rename `ShowErrorLog` → `ShowFullLog`
- [x] 5.3 Update `ViewShortcutProviderTests` in Architecture.Tests — update references from ErrorLogView to FullLogView
- [x] 5.4 Delete old `ErrorLogViewModel` tests if any exist
- [x] 5.5 Run all tests and verify green

## 6. Cleanup

- [x] 6.1 Search codebase for any remaining references to `ErrorLog`, `ErrorLogView`, `ErrorLogViewModel`, or `ErrorLogEntry` and update
- [ ] 6.2 Verify the app builds and the full log view displays all log levels with color coding
