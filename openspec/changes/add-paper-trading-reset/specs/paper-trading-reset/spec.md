## ADDED Requirements

### Requirement: Reset paper trading state
The system SHALL provide a reset command that clears all paper trading state and restores the bankroll to its initial default value ($10,000). The reset SHALL clear both in-memory state and persisted database records atomically.

#### Scenario: Successful reset clears all trades and restores bankroll
- **WHEN** user triggers the paper trading reset and confirms the action
- **THEN** all open trades SHALL be removed
- **AND** all settled trades SHALL be removed
- **AND** the bankroll SHALL be restored to $10,000
- **AND** all trade and trade result records SHALL be deleted from the database
- **AND** the persisted bankroll state SHALL be updated to $10,000

#### Scenario: Reset is rejected on cancellation
- **WHEN** user triggers the paper trading reset and selects "No" on the confirmation dialog
- **THEN** no state SHALL be modified
- **AND** no database records SHALL be changed

### Requirement: Reset only available in Paper mode
The reset command SHALL only be executable when the application is in Paper mode. The system SHALL prevent reset execution in Live mode.

#### Scenario: Reset executes in Paper mode
- **WHEN** the application is in Paper mode
- **AND** user triggers the paper trading reset
- **THEN** the confirmation dialog SHALL be displayed

#### Scenario: Reset blocked in Live mode
- **WHEN** the application is in Live mode
- **AND** user triggers the paper trading reset
- **THEN** the system SHALL not display the confirmation dialog
- **AND** no state SHALL be modified

### Requirement: Confirmation dialog before reset
The system SHALL display a modal confirmation dialog before executing the reset to prevent accidental data loss. The dialog SHALL clearly communicate the destructive nature of the action.

#### Scenario: Confirmation dialog displayed
- **WHEN** user triggers the reset command in Paper mode
- **THEN** a modal dialog SHALL appear with the message "Reset all paper trades and bankroll to $10,000?"
- **AND** the dialog SHALL present "Yes" and "No" options

### Requirement: Keyboard shortcut for reset
The system SHALL provide a keyboard shortcut `Ctrl+Shift+R` to trigger the paper trading reset from the dashboard view.

#### Scenario: Keyboard shortcut triggers reset flow
- **WHEN** user presses `Ctrl+Shift+R` on the dashboard
- **AND** the application is in Paper mode
- **THEN** the confirmation dialog SHALL be displayed

#### Scenario: Keyboard shortcut ignored in Live mode
- **WHEN** user presses `Ctrl+Shift+R` on the dashboard
- **AND** the application is in Live mode
- **THEN** no action SHALL be taken

### Requirement: Menu item for reset
The system SHALL provide a "Reset Paper Trading" menu item under the Trading menu in the menu bar.

#### Scenario: Menu item triggers reset flow
- **WHEN** user selects "Reset Paper Trading" from the Trading menu
- **AND** the application is in Paper mode
- **THEN** the confirmation dialog SHALL be displayed

#### Scenario: Menu item disabled in Live mode
- **WHEN** the application is in Live mode
- **THEN** the "Reset Paper Trading" menu item SHALL be disabled or hidden

### Requirement: Dashboard refresh after reset
After a successful reset, all dashboard views SHALL be refreshed to reflect the clean state.

#### Scenario: Views updated after reset
- **WHEN** a paper trading reset completes successfully
- **THEN** the portfolio view SHALL show the restored bankroll of $10,000
- **AND** the trades view SHALL show no recent trades
- **AND** the trades management view SHALL show no open or settled trades
- **AND** the status bar bankroll display SHALL update to $10,000

### Requirement: Audit logging for reset
The system SHALL record the reset action in the audit log, consistent with existing audit trail patterns.

#### Scenario: Audit entry created on reset
- **WHEN** a paper trading reset completes successfully
- **THEN** an audit log entry SHALL be created with action type indicating a reset
- **AND** the entry SHALL record the number of open trades cleared
- **AND** the entry SHALL record the number of settled trades cleared
- **AND** the entry SHALL record the previous bankroll value

### Requirement: User-visible log message for reset
The system SHALL display a log message in the dashboard log panel after a successful reset.

#### Scenario: Log message displayed after reset
- **WHEN** a paper trading reset completes successfully
- **THEN** a log message SHALL appear in the format: "Paper trading reset: bankroll restored to $10,000.00, N trades cleared"
- **AND** the message SHALL include the total count of open and settled trades that were removed

### Requirement: Help overlay includes reset shortcut
The `Ctrl+Shift+R` shortcut SHALL appear in the F1 keyboard shortcuts help dialog.

#### Scenario: Reset shortcut visible in help
- **WHEN** user presses F1 to view keyboard shortcuts
- **THEN** the help dialog SHALL include an entry for `Ctrl+Shift+R` with description "Reset Paper Trading"
