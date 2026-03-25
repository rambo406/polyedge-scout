## ADDED Requirements

### Requirement: ViewNavigator supports multiple full-page views
The `ViewNavigator` SHALL support navigation between three views: Dashboard, ErrorLog, and TradesManagement, using an enum-based state machine (`ActiveView`) instead of the current boolean toggle.

#### Scenario: Navigate from Dashboard to Error Log
- **WHEN** `Show(ActiveView.ErrorLog)` is called while on the Dashboard
- **THEN** the Dashboard views SHALL be hidden and the Error Log view SHALL be visible

#### Scenario: Navigate from Dashboard to Trades Management
- **WHEN** `Show(ActiveView.TradesManagement)` is called while on the Dashboard
- **THEN** the Dashboard views SHALL be hidden and the Trades Management view SHALL be visible

#### Scenario: Navigate from Error Log to Dashboard
- **WHEN** `Show(ActiveView.Dashboard)` is called while on the Error Log
- **THEN** the Error Log view SHALL be hidden and the Dashboard views SHALL be visible

#### Scenario: Navigate from Trades Management to Dashboard
- **WHEN** `Show(ActiveView.Dashboard)` is called while on the Trades Management view
- **THEN** the Trades Management view SHALL be hidden and the Dashboard views SHALL be visible

#### Scenario: Navigate from Error Log to Trades Management
- **WHEN** `Show(ActiveView.TradesManagement)` is called while on the Error Log
- **THEN** the Error Log view SHALL be hidden and the Trades Management view SHALL be visible

### Requirement: ActiveProvider returns correct shortcut provider for current view
The `ViewNavigator.ActiveProvider` property SHALL return the `IShortcutHelpProvider` for whichever view is currently active, supporting all registered views.

#### Scenario: ActiveProvider returns dashboard provider
- **WHEN** the Dashboard is the active view
- **THEN** `ActiveProvider` SHALL return the dashboard's `IShortcutHelpProvider`

#### Scenario: ActiveProvider returns error log provider
- **WHEN** the Error Log is the active view
- **THEN** `ActiveProvider` SHALL return the Error Log's `IShortcutHelpProvider`

#### Scenario: ActiveProvider returns trades management provider
- **WHEN** the Trades Management view is the active view
- **THEN** `ActiveProvider` SHALL return the Trades Management view's `IShortcutHelpProvider`

### Requirement: Backward-compatible ShowDashboard and ShowErrorLog methods
The existing `ShowDashboard()` and `ShowErrorLog()` methods SHALL continue to work, delegating to the new `Show(ActiveView)` method for backward compatibility.

#### Scenario: ShowDashboard delegates to Show
- **WHEN** `ShowDashboard()` is called
- **THEN** the behavior SHALL be identical to calling `Show(ActiveView.Dashboard)`

#### Scenario: ShowErrorLog delegates to Show
- **WHEN** `ShowErrorLog()` is called
- **THEN** the behavior SHALL be identical to calling `Show(ActiveView.ErrorLog)`
