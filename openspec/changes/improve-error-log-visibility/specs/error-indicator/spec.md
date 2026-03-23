## ADDED Requirements

### Requirement: Dashboard displays a persistent error indicator when errors occur
The dashboard SHALL display a visible error indicator bar at the top of the content area whenever an error has been logged. The indicator SHALL remain visible until cleared by a successful scan cycle or user navigation.

#### Scenario: Error indicator appears after a scan failure
- **WHEN** the scan loop catches an exception and logs an error via `LogViewModel.AddMessage`
- **THEN** the error indicator SHALL become visible at the top of the dashboard
- **AND** the indicator SHALL display the error message text with a timestamp
- **AND** the indicator SHALL use a visually distinct color scheme (red or yellow background) to differentiate from normal content

#### Scenario: Error indicator clears after a successful scan
- **WHEN** a scan cycle completes successfully after a previous error
- **THEN** the error indicator SHALL be hidden
- **AND** the dashboard layout SHALL reclaim the vertical space used by the indicator

#### Scenario: Error indicator shows the most recent error
- **WHEN** multiple errors occur in sequence
- **THEN** the error indicator SHALL display only the most recent error message
- **AND** older errors SHALL remain available in the Error Log view

#### Scenario: Error indicator is hidden when no errors exist
- **WHEN** the application starts with no errors
- **THEN** the error indicator SHALL NOT be visible
- **AND** the dashboard layout SHALL use the full available height
