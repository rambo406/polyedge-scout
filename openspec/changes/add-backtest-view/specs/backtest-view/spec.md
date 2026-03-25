## ADDED Requirements

### Requirement: BacktestView is accessible from the dashboard
The system SHALL provide a keyboard shortcut `Ctrl+B` on the dashboard that navigates to the full-page BacktestView.

#### Scenario: Navigate to BacktestView
- **WHEN** the user presses `Ctrl+B` while on the dashboard
- **THEN** the system SHALL display the BacktestView as a full-page view, hiding all dashboard panels

#### Scenario: Return to dashboard
- **WHEN** the user presses `Escape` while on the BacktestView
- **THEN** the system SHALL navigate back to the dashboard, restoring all dashboard panels

### Requirement: BacktestView displays summary metrics
The system SHALL display a summary panel at the top of the BacktestView showing: Total Markets, Markets with Edge, Brier Score, Win Rate, Edge Accuracy, and Hypothetical P&L from the most recent backtest run.

#### Scenario: Summary panel with results
- **WHEN** the BacktestView is displayed and a backtest result exists
- **THEN** the summary panel SHALL show all six metrics formatted identically to the CLI backtest command output

#### Scenario: Summary panel without results
- **WHEN** the BacktestView is displayed and no backtest has been run
- **THEN** the summary panel SHALL display placeholder values (e.g., dashes or zeros) indicating no data

### Requirement: BacktestView displays market entry table
The system SHALL display a scrollable table of individual backtest entries with columns: Market (question text), Model (probability), Market Price (yes price), Edge, Actual (outcome), and Correct (✓/✗).

#### Scenario: Table with entries
- **WHEN** a backtest result exists with entries
- **THEN** the table SHALL list all entries with properly formatted values and allow vertical scrolling

#### Scenario: Table without entries
- **WHEN** no backtest has been run
- **THEN** the table SHALL display an empty-state message such as "No backtest results. Press Ctrl+R to run."

### Requirement: User can trigger a backtest run from the view
The system SHALL provide a keyboard shortcut `Ctrl+R` within the BacktestView to initiate a backtest run using the existing `BacktestService`.

#### Scenario: Start a backtest
- **WHEN** the user presses `Ctrl+R` while on the BacktestView and no backtest is currently running
- **THEN** the system SHALL invoke `BacktestViewModel.RunBacktestAsync`, display a "Running..." status indicator, and disable further run requests

#### Scenario: Backtest completes
- **WHEN** a running backtest completes
- **THEN** the system SHALL update the summary panel and entry table with the new results and clear the running status

#### Scenario: Attempt to run while already running
- **WHEN** the user presses `Ctrl+R` while a backtest is already in progress
- **THEN** the system SHALL ignore the request (no duplicate runs)

### Requirement: BacktestView provides shortcut help
The system SHALL implement `IShortcutHelpProvider` on the BacktestView, exposing all view-specific shortcuts for the F1 help overlay.

#### Scenario: F1 help overlay
- **WHEN** the user presses `F1` while on the BacktestView
- **THEN** the help dialog SHALL display BacktestView-specific shortcuts (Escape: Return to Dashboard, Ctrl+R: Run Backtest) alongside global shortcuts

### Requirement: BacktestView integrates with ViewNavigator
The system SHALL register the BacktestView with `ViewNavigator` using a new `ActiveView.Backtest` enum member, following the same pattern as `FullLogView` and `TradesManagementView`.

#### Scenario: ViewNavigator registration
- **WHEN** the application starts
- **THEN** `BacktestView` SHALL be registered with `ViewNavigator` under `ActiveView.Backtest` and function correctly with Show/ShowDashboard navigation

### Requirement: BacktestViewModel retains results across navigation
The system SHALL register `BacktestViewModel` as a singleton so that backtest results persist when the user navigates away from and back to the BacktestView.

#### Scenario: Results persist after navigation
- **WHEN** the user runs a backtest, navigates to the dashboard, and returns to the BacktestView
- **THEN** the previously computed backtest results SHALL still be displayed
