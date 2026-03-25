## 1. ViewModel — clipboard formatting methods

- [x] 1.1 Add `GetSelectedEntryText(int index)` method to `ErrorLogViewModel` that returns the message text for the entry at the given index, or `null` if the index is out of range
- [x] 1.2 Add `GetAllEntriesText()` method to `ErrorLogViewModel` that returns all entry messages joined by `Environment.NewLine`, or `null` if the list is empty
- [x] 1.3 Write unit tests for `GetSelectedEntryText` — valid index, out-of-range index, empty list
- [x] 1.4 Write unit tests for `GetAllEntriesText` — multiple entries, single entry, empty list

## 2. View — key bindings and clipboard integration

- [x] 2.1 Handle `Ctrl+C` in `ErrorLogView.OnKeyDown`: get the currently selected index from `_listView.SelectedItem`, call `_vm.GetSelectedEntryText(index)`, and copy via `Clipboard.TrySetClipboardData`
- [x] 2.2 Handle `Ctrl+Shift+C` in `ErrorLogView.OnKeyDown`: call `_vm.GetAllEntriesText()` and copy via `Clipboard.TrySetClipboardData`
- [x] 2.3 Implement title-flash feedback: on successful copy show `"Error Log [Copied!]"`, on failure show `"Error Log [Copy failed]"`, then revert to the normal title after ~2 seconds using `Application.AddTimeout`

## 3. Help overlay registration

- [x] 3.1 Add `new ShortcutHelpItem("Ctrl+C", "Copy Selected Entry")` to `GetShortcuts()` in `ErrorLogView`
- [x] 3.2 Add `new ShortcutHelpItem("Ctrl+Shift+C", "Copy All Entries")` to `GetShortcuts()` in `ErrorLogView`
- [x] 3.3 No changes needed — architecture test already verifies IShortcutHelpProvider implementation, not shortcut content

## 4. Verification

- [x] 4.1 Run all architecture tests and confirm no regressions
- [ ] 4.2 Run the app manually and verify Ctrl+C copies the selected entry, Ctrl+Shift+C copies all entries, and F1 shows the new shortcuts
