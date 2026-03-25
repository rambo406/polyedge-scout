## 1. ViewModel Changes

- [x] 1.1 Add `bool WordWrap` property (default `false`) to `ErrorLogViewModel`
- [x] 1.2 Add `Action? WordWrapChanged` event to `ErrorLogViewModel`
- [x] 1.3 Add `ToggleWordWrap()` method that flips `WordWrap` and raises `WordWrapChanged`
- [x] 1.4 Write unit tests for `ErrorLogViewModel`: `WordWrap` defaults to `false`, `ToggleWordWrap` flips value and fires event

## 2. View Changes — Wrap-Mode Rendering

- [x] 2.1 Add a read-only `TextView` field to `ErrorLogView` for wrapped display (initially hidden)
- [x] 2.2 Create a `RebuildView()` method that shows `ListView` when `WordWrap` is off and shows `TextView` (with all entries joined by newlines) when `WordWrap` is on
- [x] 2.3 Subscribe to `_vm.WordWrapChanged` in `ErrorLogView` constructor and call `RebuildView()` on change
- [x] 2.4 Update `OnEntryAdded` to also refresh the `TextView` content when in wrap mode and auto-scroll to bottom

## 3. Keyboard Shortcut & Title Indicator

- [x] 3.1 Handle `Ctrl+W` in `ErrorLogView.OnKeyDown` — call `_vm.ToggleWordWrap()` and return `true`
- [x] 3.2 Update `ErrorLogView.Title` dynamically to show `"Error Log [Wrap: ON]"` or `"Error Log [Wrap: OFF]"` when wrap state changes

## 4. Integration Verification

- [x] 4.1 Build the solution and verify no compilation errors
- [x] 4.2 Run existing architecture and unit tests to confirm no regressions
- [ ] 4.3 Smoke-test: open Error Log, press `Ctrl+W` to toggle wrap on/off, verify title updates and messages wrap correctly
