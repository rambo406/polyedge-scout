## ADDED Requirements

### Requirement: A dedicated Error Log view displays all error and warning messages
The application SHALL provide a full-screen Error Log view that displays all `ERR` and `WRN` level log messages in a scrollable list. The view SHALL show messages in chronological order with the most recent at the bottom.

#### Scenario: Error Log view displays filtered error and warning entries
- **WHEN** the user opens the Error Log view
- **THEN** the view SHALL display all `ERR` and `WRN` messages logged during the session
- **AND** each entry SHALL show the timestamp, severity level, and full message text
- **AND** `INF` and `DBG` level messages SHALL NOT appear in this view

#### Scenario: Error Log view updates in real-time
- **WHEN** the Error Log view is open and a new error or warning is logged
- **THEN** the new entry SHALL appear at the bottom of the list
- **AND** the view SHALL auto-scroll to show the new entry if the user was already at the bottom

#### Scenario: Error Log view supports keyboard scrolling
- **WHEN** the Error Log view is focused
- **THEN** the user SHALL be able to scroll through entries using `↑`, `↓`, `Page Up`, `Page Down`, `Home`, and `End` keys

#### Scenario: Error Log view maintains a bounded buffer
- **WHEN** the number of error/warning entries exceeds the maximum buffer size (500 entries)
- **THEN** the oldest entries SHALL be removed to maintain the buffer limit
- **AND** the most recent entries SHALL always be preserved

### Requirement: Error Log view is accessible via keyboard shortcut
The Error Log view SHALL be reachable via a dedicated keyboard shortcut from any view.

#### Scenario: Ctrl+L opens the Error Log view
- **WHEN** the user presses `Ctrl+L` from the Dashboard view
- **THEN** the Error Log view SHALL be displayed as the full content area
- **AND** the MenuBar and StatusBar SHALL remain visible

#### Scenario: Escape returns to the Dashboard from the Error Log view
- **WHEN** the user presses `Escape` while the Error Log view is active
- **THEN** the Dashboard view SHALL be displayed
- **AND** the Dashboard state (market data, portfolio, etc.) SHALL be preserved

### Requirement: Error Log view is accessible via the menu
The Error Log view SHALL be accessible through the application menu bar.

#### Scenario: View menu contains Error Log item
- **WHEN** the user opens the View menu in the MenuBar
- **THEN** a menu item labeled "Error Log" with the accelerator `Ctrl+L` SHALL be present
- **AND** selecting the menu item SHALL open the Error Log view
