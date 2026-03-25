## Why

Users currently have no way to discover available keyboard shortcuts without reading source code or documentation. The TUI app has multiple views (Dashboard, Error Log) each with their own shortcuts (`Ctrl+Q`, `Ctrl+L`, `F5`, `Ctrl+T`, `Ctrl+W`, `Escape`), but nothing in the UI tells users what's available on their current page. A help overlay triggered by a universal key would make the app self-documenting and reduce the learning curve.

## What Changes

- Add a keyboard shortcuts help dialog/overlay that can be triggered from any page via a universal key (e.g., `?` or `F1`)
- The overlay displays page-specific shortcuts — showing only shortcuts relevant to the currently active view (Dashboard vs Error Log)
- The overlay is dismissible via `Escape` or the same trigger key
- Each view provides its own shortcut definitions so the help content stays in sync with actual key bindings
- The trigger key does not conflict with existing shortcuts (`Ctrl+Q`, `Ctrl+L`, `F5`, `Ctrl+T`, `Ctrl+W`, `Escape`)

## Capabilities

### New Capabilities
- `keyboard-shortcuts-help`: A reusable help overlay/dialog that displays context-sensitive keyboard shortcut listings per view, triggered by a universal key binding

### Modified Capabilities

_None — no existing spec-level requirements are changing._

## Impact

- **Views**: `MainWindow`, `ErrorLogView`, and potentially all view classes need to register their shortcuts
- **App layer**: `ViewNavigator` or `MainWindow` needs awareness of which view is active to show the correct help content
- **New files**: A `ShortcutHelpDialog` view class and a shortcut registration/definition mechanism
- **Dependencies**: No new external dependencies — uses Terminal.Gui v2 `Dialog` or `Window`
- **Existing shortcuts**: No changes to existing key bindings; the trigger key must be chosen to avoid conflicts
