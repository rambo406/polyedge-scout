## 1. Foundation Types

- [x] 1.1 Create `ShortcutHelpItem` record in `PolyEdgeScout.Console/App/` with `Key` and `Description` properties
- [x] 1.2 Create `IShortcutHelpProvider` interface in `PolyEdgeScout.Console/App/` with `IReadOnlyList<ShortcutHelpItem> GetShortcuts()` method

## 2. Help Dialog

- [x] 2.1 Create `ShortcutHelpDialog` view class in `PolyEdgeScout.Console/Views/` that extends Terminal.Gui `Dialog`
- [x] 2.2 Accept a list of `ShortcutHelpItem` entries in the constructor and display them in a formatted two-column layout (Key | Description)
- [x] 2.3 Handle `Escape` and `F1` to close the dialog

## 3. View Shortcut Providers

- [x] 3.1 Implement `IShortcutHelpProvider` on `MainWindow` (or a dedicated dashboard provider) returning Dashboard shortcuts: `F5` Refresh, `Ctrl+T` Toggle Mode, `Ctrl+L` Error Log
- [x] 3.2 Implement `IShortcutHelpProvider` on `ErrorLogView` returning Error Log shortcuts: `Escape` Return to Dashboard, `Ctrl+W` Toggle Word Wrap
- [x] 3.3 Define global shortcuts list (available on all pages): `Ctrl+Q` Quit, `F1` Help

## 4. Active View Awareness

- [x] 4.1 Add an `ActiveView` property or method to `ViewNavigator` that returns the current active view (Dashboard or Error Log)
- [x] 4.2 Update `ShowDashboard()` and `ShowErrorLog()` to track which view is currently active

## 5. F1 Key Binding and Integration

- [x] 5.1 Register `F1` key handler at the `MainWindow` level to intercept the help trigger globally
- [x] 5.2 On `F1`, query `ViewNavigator` for the active view, get its `IShortcutHelpProvider` shortcuts, combine with global shortcuts, and open `ShortcutHelpDialog`
- [x] 5.3 Add a "Help (F1)" entry to the menu bar under a Help menu or the View menu via `MenuBarFactory`

## 6. Testing

- [x] 6.1 Skipped — ShortcutHelpDialog extends Terminal.Gui Dialog, requires Application.Init() which is impractical for unit tests
- [x] 6.2 Skipped — MainWindow and ErrorLogView extend Terminal.Gui views, cannot be constructed in unit tests
- [x] 6.3 Add unit tests verifying `ViewNavigator.ActiveView` tracks view switches correctly
- [x] 6.4 Add architecture test verifying all views with `OnKeyDown` overrides implement `IShortcutHelpProvider`
