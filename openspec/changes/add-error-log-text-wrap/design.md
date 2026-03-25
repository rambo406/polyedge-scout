## Context

PolyEdge Scout is a Terminal.Gui v2 TUI for scanning prediction markets. The Error Log view (`ErrorLogView`) was introduced in the `improve-error-log-visibility` change. It displays ERR and WRN log entries in a full-screen `FrameView` containing a `ListView`. Each entry is rendered as a single line via `ErrorLogViewModel.Entries`.

Currently, long error messages (stack traces, verbose API responses) are truncated at the view's right edge. The `ListView` does not wrap text — each entry occupies exactly one row. Users must open the log file externally to read full messages.

The existing architecture follows MVVM: `ErrorLogViewModel` holds the data (`IReadOnlyList<ErrorLogEntry> Entries`, `EntryAdded` event), and `ErrorLogView` subscribes and renders. Navigation is handled by `ViewNavigator` (toggle visibility, `Ctrl+L` / `Escape`).

## Goals / Non-Goals

**Goals:**
- Allow users to toggle between single-line (no-wrap) and multi-line (word-wrap) rendering of error messages in the Error Log view
- Provide a keyboard shortcut (`Ctrl+W`) to toggle wrap mode without leaving the view
- Display the current wrap state visually so users know which mode is active
- Default to no-wrap (current behavior) so existing UX is unchanged

**Non-Goals:**
- Per-entry wrap/expand (accordion-style) — all entries follow the same wrap mode
- Persisting the wrap preference across sessions — always starts as no-wrap
- Horizontal scrolling in no-wrap mode (current `ListView` truncation behavior is acceptable)
- Changing the Error Log's filtering, search, or entry format

## Decisions

### 1. Use Terminal.Gui `TextView` in read-only mode for wrapped display

**Decision**: When word-wrap is enabled, replace the `ListView` content rendering with a single read-only `TextView` that has `WordWrap = true`. The `TextView` displays all entries separated by newlines. When word-wrap is disabled, use the existing `ListView`.

**Rationale**: Terminal.Gui's `ListView` renders one item per row with no built-in word-wrap. `TextView` natively supports `WordWrap` and is the simplest path to multi-line rendering. Toggling between the two controls avoids fighting `ListView`'s design.

**Alternative considered**: Custom `ListView` cell rendering with manual line-splitting — rejected due to complexity of calculating row heights and managing selection. A `TableView` was also considered but adds unnecessary overhead for a simple text display.

### 2. Toggle state lives on `ErrorLogViewModel`

**Decision**: Add a `bool WordWrap` property and `Action? WordWrapChanged` event to `ErrorLogViewModel`. The view reads this property to decide which rendering mode to use.

**Rationale**: Keeps view logic thin (MVVM). The ViewModel owns the state; the View observes and renders. This also makes the toggle testable without UI dependencies.

### 3. Keyboard shortcut `Ctrl+W` handled in `ErrorLogView`

**Decision**: Handle `Ctrl+W` in `ErrorLogView.OnKeyDown`. When pressed, toggle `_vm.WordWrap` and re-render. No menu item needed — the toggle is only relevant inside the Error Log view.

**Rationale**: `Ctrl+W` is intuitive ("W" for wrap) and follows the existing pattern of `ErrorLogView` handling `Escape` for navigation. Adding a menu item creates unnecessary complexity for a view-local toggle.

**Alternative considered**: Adding it to `MenuBarFactory` under "View" menu — viable but overly formal for a view-specific setting. Can be added later if needed.

### 4. Visual indicator in the view title

**Decision**: Update the `ErrorLogView.Title` to include the wrap state, e.g., `"Error Log [Wrap: ON]"` or `"Error Log [Wrap: OFF]"`. Change the title dynamically when toggling.

**Rationale**: The `FrameView.Title` is always visible and requires no additional UI elements. It clearly communicates the current mode.

**Alternative considered**: A `Label` or `StatusBar` indicator — adds layout complexity for minimal benefit.

## Risks / Trade-offs

- **[Risk] `TextView` may have different scrolling/selection UX than `ListView`** → Mitigation: `TextView` is read-only and supports keyboard scrolling natively. Selection is not a critical feature for log viewing.
- **[Trade-off] Toggling between `ListView` and `TextView` causes a brief visual reset** → Acceptable because the toggle is user-initiated and the view rebuilds instantly. Auto-scroll to bottom is preserved.
- **[Risk] Very large log buffers (500 entries × long messages) may cause `TextView` performance issues** → Mitigation: The 500-entry cap on `ErrorLogViewModel` bounds the data. If needed, the `TextView` content can be lazily built only when wrap mode is active.
