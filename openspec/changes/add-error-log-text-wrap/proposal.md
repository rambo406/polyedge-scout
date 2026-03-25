## Why

The Error Log view displays error and warning messages in a `ListView` where each entry is a single line. Long error messages (e.g., stack traces, verbose API error responses) are truncated at the view boundary with no way to read the full text. Users must currently rely on the log file to see complete messages, which breaks the workflow of triaging errors in-app.

## What Changes

- Add a **text wrap toggle** to `ErrorLogView` that switches between single-line (truncated/horizontal-scroll) and multi-line (wrapped) rendering of error messages.
- Introduce a `WordWrap` property on `ErrorLogViewModel` to track the toggle state.
- Bind a keyboard shortcut (`Ctrl+W`) to toggle wrapping on/off directly from the Error Log view.
- Display current wrap state in the view's status area or title so users know which mode is active.

## Capabilities

### New Capabilities

- `error-log-text-wrap`: Toggle text wrapping on/off for error messages in the Error Log view, allowing users to read full message content without leaving the TUI.

### Modified Capabilities

_(none — no existing spec-level requirements are changing)_

## Impact

- **`ErrorLogView.cs`** — Replace or augment `ListView` rendering to support wrapped text mode; add keybinding for toggle.
- **`ErrorLogViewModel.cs`** — Add `WordWrap` boolean property and `WordWrapChanged` event.
- **No Domain/Application/Infrastructure changes** — this is purely a Console-layer UI enhancement.
- **No breaking changes** — existing Error Log behavior (single-line list) remains the default.
