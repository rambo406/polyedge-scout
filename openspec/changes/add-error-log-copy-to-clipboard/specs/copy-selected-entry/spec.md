## ADDED Requirements

### Requirement: Copy selected error log entry to clipboard
The system SHALL copy the currently selected error log entry's message text to the system clipboard when the user presses **Ctrl+C** in the Error Log view.

#### Scenario: Copy selected entry in list mode
- **WHEN** the Error Log view is in list mode (word wrap OFF) and the user has an entry selected and presses Ctrl+C
- **THEN** the selected entry's message text SHALL be placed on the system clipboard and the view SHALL display a brief "Copied!" status indicator

#### Scenario: Copy selected entry in wrap mode
- **WHEN** the Error Log view is in wrap mode (word wrap ON) and the user presses Ctrl+C
- **THEN** the currently selected entry (by index) SHALL be copied to the clipboard

#### Scenario: No entries in log
- **WHEN** the Error Log contains no entries and the user presses Ctrl+C
- **THEN** no clipboard operation SHALL occur and no error SHALL be shown

#### Scenario: Clipboard unavailable
- **WHEN** the user presses Ctrl+C but the system clipboard is not available
- **THEN** the view SHALL display a brief failure indicator (e.g., "Copy failed") in the title bar

### Requirement: Shortcut registered in help overlay
The Ctrl+C shortcut SHALL be listed in the `IShortcutHelpProvider` shortcuts returned by the Error Log view so it appears in the F1 help overlay.

#### Scenario: Help overlay shows copy shortcut
- **WHEN** the user presses F1 while on the Error Log view
- **THEN** the help overlay SHALL include an entry for "Ctrl+C" with description "Copy Selected Entry"
