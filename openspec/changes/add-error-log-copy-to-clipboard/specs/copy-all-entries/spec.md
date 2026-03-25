## ADDED Requirements

### Requirement: Copy all error log entries to clipboard
The system SHALL copy all error log entries to the system clipboard when the user presses **Ctrl+Shift+C** in the Error Log view. Entries SHALL be separated by newlines and ordered chronologically (oldest first).

#### Scenario: Copy all entries
- **WHEN** the Error Log view contains one or more entries and the user presses Ctrl+Shift+C
- **THEN** all entry message texts SHALL be joined with newline separators and placed on the system clipboard, and the view SHALL display a brief "Copied all!" status indicator

#### Scenario: No entries in log
- **WHEN** the Error Log contains no entries and the user presses Ctrl+Shift+C
- **THEN** no clipboard operation SHALL occur and no error SHALL be shown

#### Scenario: Clipboard unavailable
- **WHEN** the user presses Ctrl+Shift+C but the system clipboard is not available
- **THEN** the view SHALL display a brief failure indicator (e.g., "Copy failed") in the title bar

### Requirement: Bulk copy shortcut registered in help overlay
The Ctrl+Shift+C shortcut SHALL be listed in the `IShortcutHelpProvider` shortcuts returned by the Error Log view so it appears in the F1 help overlay.

#### Scenario: Help overlay shows bulk copy shortcut
- **WHEN** the user presses F1 while on the Error Log view
- **THEN** the help overlay SHALL include an entry for "Ctrl+Shift+C" with description "Copy All Entries"
