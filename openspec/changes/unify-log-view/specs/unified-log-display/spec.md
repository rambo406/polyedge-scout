## ADDED Requirements

### Requirement: Full log view displays all log levels
The system SHALL display ALL log levels (ERR, WRN, INF, DBG) in the full-page log view. The view SHALL NOT filter out any log level.

#### Scenario: All log levels appear in full log view
- **WHEN** the system logs messages at ERR, WRN, INF, and DBG levels
- **AND** the user opens the full log view
- **THEN** all four messages SHALL be visible in the log view in chronological order

#### Scenario: Trade reasoning messages appear in full log view
- **WHEN** the system logs trade reasoning at INF or DBG level
- **AND** the user opens the full log view
- **THEN** the trade reasoning messages SHALL be visible alongside error and warning messages

### Requirement: Full log view accepts entries via OnLogEntry without filtering
The `FullLogViewModel` SHALL accept all log entries via `OnLogEntry(string level, string formattedLine)` without filtering by level.

#### Scenario: FullLogViewModel accepts INF level
- **WHEN** `OnLogEntry` is called with level "INF"
- **THEN** the entry SHALL be added to the entries collection

#### Scenario: FullLogViewModel accepts DBG level
- **WHEN** `OnLogEntry` is called with level "DBG"
- **THEN** the entry SHALL be added to the entries collection

#### Scenario: FullLogViewModel accepts ERR level
- **WHEN** `OnLogEntry` is called with level "ERR"
- **THEN** the entry SHALL be added to the entries collection

#### Scenario: FullLogViewModel accepts WRN level
- **WHEN** `OnLogEntry` is called with level "WRN"
- **THEN** the entry SHALL be added to the entries collection

### Requirement: Full log view maintains bounded buffer
The `FullLogViewModel` SHALL maintain a bounded buffer of 500 entries, removing the oldest entry when the limit is exceeded.

#### Scenario: Buffer trims oldest entry at capacity
- **WHEN** the buffer contains 500 entries
- **AND** a new entry is added
- **THEN** the oldest entry SHALL be removed and the new entry SHALL be added at the end

### Requirement: Full log view retains word wrap toggle
The full log view SHALL support toggling word wrap via `Ctrl+W`, switching between `ListView` (no wrap) and `TextView` (wrap) display modes.

#### Scenario: Toggle word wrap on
- **WHEN** the user presses `Ctrl+W` while word wrap is off
- **THEN** the view SHALL switch to wrapped text display mode
- **AND** the title SHALL show "Log [Wrap: ON]"

#### Scenario: Toggle word wrap off
- **WHEN** the user presses `Ctrl+W` while word wrap is on
- **THEN** the view SHALL switch to list display mode
- **AND** the title SHALL show "Log [Wrap: OFF]"

### Requirement: Full log view retains copy to clipboard
The full log view SHALL support copying a single selected entry via `Ctrl+C` and all entries via `Ctrl+Shift+C`.

#### Scenario: Copy selected entry
- **WHEN** the user selects a log entry and presses `Ctrl+C`
- **THEN** the selected entry's message text SHALL be copied to the clipboard
- **AND** the title SHALL briefly flash "Log [Copied!]"

#### Scenario: Copy all entries
- **WHEN** the user presses `Ctrl+Shift+C`
- **THEN** all log entry messages SHALL be copied to the clipboard joined by newlines

### Requirement: Full log view provides shortcut help
The full log view SHALL implement `IShortcutHelpProvider` and provide shortcut descriptions for Escape, Ctrl+W, Ctrl+C, and Ctrl+Shift+C.

#### Scenario: Shortcut help lists all view shortcuts
- **WHEN** the user presses F1 while the full log view is active
- **THEN** the help dialog SHALL display shortcuts for Escape, Ctrl+W, Ctrl+C, and Ctrl+Shift+C

### Requirement: Escape returns to dashboard
The full log view SHALL return to the dashboard when the user presses `Escape`.

#### Scenario: Escape navigates back
- **WHEN** the user presses `Escape` in the full log view
- **THEN** the `ViewNavigator` SHALL switch back to the dashboard view

### Requirement: Ctrl+L opens full log view
The `MainWindow` SHALL open the full log view when the user presses `Ctrl+L`.

#### Scenario: Ctrl+L from dashboard
- **WHEN** the user presses `Ctrl+L` from the dashboard
- **THEN** the full log view SHALL become visible and receive focus
- **AND** the dashboard views SHALL be hidden
