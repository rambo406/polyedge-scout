## Context

PolyEdge Scout is a .NET Terminal.Gui v2 TUI application with two views managed by `ViewNavigator`: the Dashboard (composed of MarketTableView, PortfolioView, TradesView, LogPanelView, ErrorIndicatorView) and the ErrorLogView. Keyboard shortcuts are currently scattered across individual view classes and `MenuBarFactory` with no discoverability mechanism. Users must read code or documentation to learn shortcuts.

Current shortcut assignments:
- **Dashboard**: `Ctrl+Q` (quit), `F5` (refresh), `Ctrl+L` (open error log), `Ctrl+T` (toggle trading mode)
- **Error Log**: `Escape` (return to dashboard), `Ctrl+W` (toggle word wrap)
- **Global (via menu)**: Same as Dashboard shortcuts

`MainWindow` composes all views and creates the `ViewNavigator`. The status bar at the bottom already shows some shortcut hints but is not comprehensive.

## Goals / Non-Goals

**Goals:**
- Provide a universal trigger key that opens a context-sensitive shortcuts help overlay from any page
- Show only the shortcuts relevant to the currently active view
- Keep shortcut definitions co-located with view logic so they stay in sync
- Make the overlay reusable and easy to extend when new views or shortcuts are added

**Non-Goals:**
- Customizable or rebindable keyboard shortcuts
- Persistent user preferences for the help overlay (e.g., "don't show again")
- Searchable/filterable shortcut lists
- Accessibility beyond what Terminal.Gui provides by default

## Decisions

### 1. Trigger Key: `F1`

**Choice**: Use `F1` to open the shortcuts help overlay.

**Rationale**: `F1` is the universal convention for help across terminal applications, IDEs, and operating systems. The `?` key could conflict with future text input fields. `F1` is not currently bound to any action in the app.

**Alternatives considered**:
- `?` — natural for help, but conflicts if any text input view is added; requires special handling for focused text fields
- `Ctrl+H` — some terminals intercept this as backspace
- `Ctrl+/` — non-standard, less discoverable

### 2. Overlay Type: Terminal.Gui `Dialog`

**Choice**: Use a `Dialog` (modal) to display shortcuts.

**Rationale**: A modal dialog naturally blocks input to the underlying view, preventing accidental shortcut triggers while reading help. Terminal.Gui `Dialog` supports `Escape` to close by default and handles focus management automatically. It overlays the current content without changing view state.

**Alternatives considered**:
- `Window` overlay with manual z-ordering — more complex, no built-in dismiss behavior
- `FrameView` toggled visible — would need manual focus handling and could interfere with view navigation

### 3. Shortcut Registration: `IShortcutHelpProvider` Interface

**Choice**: Define an `IShortcutHelpProvider` interface that views implement to supply their shortcut definitions.

```csharp
public interface IShortcutHelpProvider
{
    IReadOnlyList<ShortcutHelpItem> GetShortcuts();
}

public record ShortcutHelpItem(string Key, string Description);
```

**Rationale**: Each view already knows its own shortcuts (they're hardcoded in `OnKeyDown` overrides and `MenuBarFactory`). Having views implement this interface keeps shortcut documentation co-located with the binding code. When a developer adds a new shortcut, they update `GetShortcuts()` in the same class. Global shortcuts (from `MenuBarFactory`) can be provided by a separate "global" provider.

**Alternatives considered**:
- Central registry/dictionary — easier to implement but shortcuts drift out of sync with actual bindings
- Reflection-based discovery — brittle, Terminal.Gui doesn't use attributes for key bindings

### 4. Context Awareness via `ViewNavigator`

**Choice**: `ViewNavigator` tracks which view is currently active and provides the appropriate shortcut list when the help dialog opens.

**Rationale**: `ViewNavigator` already manages which view is visible (Dashboard vs Error Log). It's the natural place to query "which view is showing" and to ask that view's `IShortcutHelpProvider` for shortcuts. The help dialog combines global shortcuts with page-specific shortcuts.

### 5. Help Dialog Composition

The dialog will show shortcuts in two sections:
1. **Page shortcuts** — from the currently active view's `IShortcutHelpProvider`
2. **Global shortcuts** — always shown (Quit, Help itself)

Formatted as a two-column table: `Key` | `Description`, rendered in a `TableView` or formatted `TextView` inside the dialog.

## Risks / Trade-offs

- **[Shortcut drift]** → Mitigated by the `IShortcutHelpProvider` interface pattern: developers update shortcuts in the same file where they define key bindings. Could add architecture tests to verify providers return non-empty lists.
- **[F1 terminal interception]** → Some terminal emulators capture `F1` for their own help. Mitigation: the status bar and menu bar can also include a "Help" entry as a mouse-accessible fallback.
- **[Dialog sizing]** → If shortcut lists grow long, the dialog may not fit small terminals. Mitigation: use a scrollable `ListView` inside the dialog.
- **[Interface adoption cost]** → Adding `IShortcutHelpProvider` requires changes to existing view classes. Mitigation: the interface has a single method with a simple return type, minimal effort per view.
