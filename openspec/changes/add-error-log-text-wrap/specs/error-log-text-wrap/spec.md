## ADDED Requirements

### Requirement: User can toggle text wrapping in the Error Log view
The Error Log view SHALL provide a toggle that switches between no-wrap (single-line per entry) and word-wrap (multi-line) rendering of error messages.

#### Scenario: Default state is no-wrap
- **WHEN** the user opens the Error Log view
- **THEN** error messages SHALL be displayed in single-line mode (no word wrapping), matching the current behavior

#### Scenario: Toggle wrap on via keyboard shortcut
- **WHEN** the user presses `Ctrl+W` while the Error Log view is active
- **AND** word wrap is currently off
- **THEN** the view SHALL switch to word-wrap mode, displaying full message text wrapped within the view width

#### Scenario: Toggle wrap off via keyboard shortcut
- **WHEN** the user presses `Ctrl+W` while the Error Log view is active
- **AND** word wrap is currently on
- **THEN** the view SHALL switch back to single-line (no-wrap) mode

#### Scenario: Wrap mode indicator displayed in title
- **WHEN** the word wrap toggle is changed
- **THEN** the view title SHALL reflect the current mode (e.g., "Error Log [Wrap: ON]" or "Error Log [Wrap: OFF]")

### Requirement: Wrapped view displays all log entries
When word-wrap mode is active, the Error Log view SHALL display all current log entries with their full message text, wrapped to the view width, separated by line breaks.

#### Scenario: All entries visible in wrap mode
- **WHEN** word wrap is enabled
- **AND** there are entries in the Error Log
- **THEN** all entries SHALL be rendered with their complete message text visible (no truncation)

#### Scenario: New entries appear in wrap mode
- **WHEN** word wrap is enabled
- **AND** a new error or warning log entry is added
- **THEN** the wrapped view SHALL update to include the new entry
- **AND** the view SHALL auto-scroll to show the latest entry

### Requirement: ViewModel exposes word-wrap state
The `ErrorLogViewModel` SHALL expose a `WordWrap` boolean property and a `WordWrapChanged` event so that the view can observe and react to wrap-state changes.

#### Scenario: WordWrap property defaults to false
- **WHEN** `ErrorLogViewModel` is created
- **THEN** the `WordWrap` property SHALL be `false`

#### Scenario: WordWrapChanged event fires on toggle
- **WHEN** the `WordWrap` property is changed
- **THEN** the `WordWrapChanged` event SHALL be raised
