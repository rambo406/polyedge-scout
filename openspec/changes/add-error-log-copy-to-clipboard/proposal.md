## Why

Users viewing error or warning entries in the Error Log screen have no way to extract those messages for sharing, pasting into bug reports, or further analysis. They must manually retype or screenshot the console output. Adding a copy-to-clipboard shortcut removes this friction and makes the Error Log actionable.

## What Changes

- Add a **Ctrl+C** keyboard shortcut that copies the currently selected error log entry to the system clipboard.
- Add a **Ctrl+Shift+C** keyboard shortcut that copies **all** error log entries to the clipboard (bulk copy).
- Register both new shortcuts in the `IShortcutHelpProvider` implementation so they appear in the F1 help overlay.
- Use Terminal.Gui's built-in `Clipboard.TrySetClipboardData` for cross-platform clipboard access.

## Capabilities

### New Capabilities
- `copy-selected-entry`: Copy the currently selected error log entry to the system clipboard via Ctrl+C.
- `copy-all-entries`: Copy all error log entries to the system clipboard via Ctrl+Shift+C.

### Modified Capabilities
<!-- No existing specs are being modified. -->

## Impact

- **Code**: `ErrorLogView.cs` — new key bindings in `OnKeyDown`, updated `GetShortcuts()` return list. `ErrorLogViewModel.cs` — new methods to format entries for clipboard output.
- **Dependencies**: Uses `Terminal.Gui.Clipboard` (already available via Terminal.Gui v2).
- **Systems**: Relies on OS clipboard support; Terminal.Gui handles platform detection internally.
