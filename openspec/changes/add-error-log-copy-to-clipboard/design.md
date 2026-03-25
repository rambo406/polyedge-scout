## Context

The Error Log view (`ErrorLogView.cs`) is a full-screen `FrameView` that displays error and warning log entries. It currently supports two display modes: a `ListView` for single-line entries and a `TextView` for word-wrapped output. The view model (`ErrorLogViewModel`) maintains a bounded buffer of up to 500 `ErrorLogEntry` records, each containing a `Timestamp`, `Level`, and `Message`.

Users can browse entries but have no way to extract them. The view already handles `Escape` (navigate back) and `Ctrl+W` (toggle word wrap) via `OnKeyDown`, and implements `IShortcutHelpProvider` for the F1 help overlay.

Terminal.Gui v2 provides `Clipboard.TrySetClipboardData(string)` for cross-platform clipboard access.

## Goals / Non-Goals

**Goals:**
- Allow users to copy a single selected error log entry to the system clipboard.
- Allow users to copy all error log entries to the system clipboard.
- Register new shortcuts in the help overlay via `IShortcutHelpProvider`.
- Provide visual feedback (brief status bar message or title flash) when a copy succeeds or fails.

**Non-Goals:**
- Multi-select / range-select of individual entries (not supported by current ListView usage).
- Filtering or searching entries before copying.
- Clipboard paste or import of entries.
- Customizable key bindings; shortcuts are hardcoded.

## Decisions

### 1. Key bindings: Ctrl+C (selected) / Ctrl+Shift+C (all)

**Rationale**: `Ctrl+C` is the universal copy shortcut. In a read-only log view there is no text-editing context where Ctrl+C would conflict. `Ctrl+Shift+C` extends the pattern for "copy all" — it is discoverable and mnemonic.

**Alternative considered**: Using `Ctrl+Y` or a function key. Rejected because `Ctrl+C` is universally understood for copy; deviating would confuse users.

### 2. Clipboard formatting: plain text, one entry per line

Selected entries are copied as their `Message` string. For bulk copy each entry is separated by a newline. No timestamp prefix is added beyond what is already in the formatted `Message` (which already includes timestamp and level from the log formatter).

**Alternative considered**: Structured formats (JSON, CSV). Rejected as over-engineering for a console-based debugging tool. Users pasting into chat or issue trackers want plain text.

### 3. Copy logic lives in the ViewModel

`ErrorLogViewModel` exposes two methods — `GetSelectedEntryText(int index)` and `GetAllEntriesText()` — returning formatted strings. The view calls `Clipboard.TrySetClipboardData` with the result. This keeps the view thin and the formatting testable.

**Alternative considered**: Putting clipboard calls directly in the view. Rejected because the ViewModel can be unit-tested without Terminal.Gui dependencies.

### 4. Visual feedback via title bar text

After a copy operation the view title temporarily changes to include a status indicator (e.g., `"Error Log [Copied!]"`) and reverts after ~2 seconds. This avoids adding new UI elements (status bars, dialogs) while providing clear feedback.

**Alternative considered**: `MessageBox.Query`. Rejected because a modal dialog for a non-blocking operation is disruptive.

## Risks / Trade-offs

- **[Clipboard unavailable on headless/SSH sessions]** → Terminal.Gui's `Clipboard.TrySetClipboardData` returns `false` when no clipboard provider is available. The view will show a `"Copy failed — clipboard unavailable"` title flash so the user knows it didn't work. No crash path.
- **[Ctrl+C intercepted by terminal emulator]** → Some terminals treat `Ctrl+C` as SIGINT. Terminal.Gui v2 runs in raw/cooked mode and captures key events before the terminal acts, so this is not expected to be an issue. If it surfaces in testing, fallback to an alternate binding (e.g., `Ctrl+Insert`).
- **[Large log buffer]** → Copying 500 entries to clipboard could be a large string. At ~200 chars per line this is ~100 KB, well within clipboard limits. No mitigation needed.
