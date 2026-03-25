## ADDED Requirements

### Requirement: Full-page trades management view displays all open trades
The system SHALL provide a full-page view that displays ALL open trades from `IOrderService.OpenTrades` in a scrollable `TableView` with columns: Market Question, Action, Status, Entry Price, Shares, Outlay, Edge, Timestamp.

#### Scenario: View shows all open trades
- **WHEN** the user opens the Trades Management view and there are 76 open trades
- **THEN** the view SHALL display all 76 trades in a scrollable table with correct column data

#### Scenario: View shows empty state when no open trades exist
- **WHEN** the user opens the Trades Management view and there are no open trades
- **THEN** the "Open Trades" tab SHALL display a message "No open trades"

#### Scenario: Market question column truncation
- **WHEN** a trade's market question exceeds 40 characters
- **THEN** the column SHALL display the first 39 characters followed by "…"

### Requirement: Full-page trades management view displays settled trade history
The system SHALL display all settled trades (trade results) in a separate tab within the Trades Management view, with columns: Market Question, Won/Lost, Entry Price, Shares, Net Profit, ROI, Settled At.

#### Scenario: View shows all settled trades
- **WHEN** the user opens the Trades Management view and switches to the "Settled Trades" tab
- **THEN** the view SHALL display all settled trades ordered by settlement date (most recent first)

#### Scenario: Empty settled trades
- **WHEN** there are no settled trades
- **THEN** the "Settled Trades" tab SHALL display a message "No settled trades"

### Requirement: Trades management view is accessible via keyboard shortcut
The system SHALL open the Trades Management view when the user presses `Ctrl+Shift+T` from any active view.

#### Scenario: Open from dashboard
- **WHEN** the user presses `Ctrl+Shift+T` while on the dashboard
- **THEN** the system SHALL navigate to the full-page Trades Management view, hiding the dashboard

#### Scenario: Open from error log
- **WHEN** the user presses `Ctrl+Shift+T` while on the error log view
- **THEN** the system SHALL navigate to the Trades Management view, hiding the error log

### Requirement: Trades management view supports navigation back to dashboard
The system SHALL return to the dashboard when the user presses `Escape` from the Trades Management view.

#### Scenario: Escape returns to dashboard
- **WHEN** the user presses `Escape` while on the Trades Management view
- **THEN** the system SHALL show the dashboard and hide the Trades Management view

### Requirement: Trades management view provides shortcut help
The Trades Management view SHALL implement `IShortcutHelpProvider` so that pressing `F1` while on the view shows its available shortcuts.

#### Scenario: F1 shows trades management shortcuts
- **WHEN** the user presses `F1` while on the Trades Management view
- **THEN** the help overlay SHALL display shortcuts specific to the Trades Management view including at minimum: Escape (Back to Dashboard)

### Requirement: Trade data refreshes on scan cycle
The Trades Management view SHALL update its displayed data when a new scan cycle completes.

#### Scenario: Data refresh after scan
- **WHEN** a scan cycle completes and the Trades Management view is active
- **THEN** the view SHALL reflect the current open trades and settled trades without user intervention

### Requirement: Dashboard trades panel title clarification
The existing dashboard "Last 5 Trades" panel title SHALL be renamed to "Recent Settlements" to accurately reflect that it shows settled trade results, not open positions.

#### Scenario: Panel title is accurate
- **WHEN** the dashboard is displayed
- **THEN** the trades panel SHALL have the title "Recent Settlements"

#### Scenario: Panel hints at full view
- **WHEN** there are trades displayed or no trades yet in the dashboard panel
- **THEN** the panel SHALL include a hint line "(Ctrl+Shift+T for all trades)"
