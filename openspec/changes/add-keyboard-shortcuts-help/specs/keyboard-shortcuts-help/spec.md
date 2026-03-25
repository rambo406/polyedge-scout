## ADDED Requirements

### Requirement: Universal help trigger opens shortcuts overlay
The system SHALL open a keyboard shortcuts help overlay when the user presses `F1` from any view in the application.

#### Scenario: Opening help from Dashboard
- **WHEN** the user presses `F1` while the Dashboard view is active
- **THEN** the system SHALL display a modal help dialog showing all available keyboard shortcuts for the Dashboard

#### Scenario: Opening help from Error Log
- **WHEN** the user presses `F1` while the Error Log view is active
- **THEN** the system SHALL display a modal help dialog showing all available keyboard shortcuts for the Error Log

### Requirement: Help overlay shows page-specific shortcuts
The help overlay SHALL display shortcuts that are specific to the currently active view, combined with global shortcuts that are always available.

#### Scenario: Dashboard shortcuts displayed
- **WHEN** the help overlay is opened from the Dashboard view
- **THEN** the overlay SHALL display Dashboard-specific shortcuts including Refresh (`F5`), Toggle Mode (`Ctrl+T`), and Error Log (`Ctrl+L`)
- **AND** the overlay SHALL display global shortcuts including Quit (`Ctrl+Q`) and Help (`F1`)

#### Scenario: Error Log shortcuts displayed
- **WHEN** the help overlay is opened from the Error Log view
- **THEN** the overlay SHALL display Error Log-specific shortcuts including Return to Dashboard (`Escape`) and Toggle Word Wrap (`Ctrl+W`)
- **AND** the overlay SHALL display global shortcuts including Quit (`Ctrl+Q`) and Help (`F1`)

### Requirement: Help overlay is dismissible
The help overlay SHALL be dismissible without side effects, returning the user to their previous view state.

#### Scenario: Dismiss with Escape
- **WHEN** the help overlay is open and the user presses `Escape`
- **THEN** the overlay SHALL close and the underlying view SHALL remain unchanged

#### Scenario: Dismiss with trigger key
- **WHEN** the help overlay is open and the user presses `F1`
- **THEN** the overlay SHALL close and the underlying view SHALL remain unchanged

### Requirement: Help trigger does not conflict with existing shortcuts
The `F1` help trigger SHALL not interfere with or override any existing keyboard shortcuts in the application.

#### Scenario: Existing shortcuts remain functional
- **WHEN** the help overlay is closed
- **THEN** all existing shortcuts (`Ctrl+Q`, `F5`, `Ctrl+L`, `Ctrl+T`, `Ctrl+W`, `Escape`) SHALL continue to function as before

### Requirement: Views provide shortcut definitions via IShortcutHelpProvider
Each view that has keyboard shortcuts SHALL implement `IShortcutHelpProvider` to supply its shortcut definitions to the help system.

#### Scenario: View returns its shortcuts
- **WHEN** the help system queries a view implementing `IShortcutHelpProvider`
- **THEN** the view SHALL return a non-empty list of `ShortcutHelpItem` entries describing all its keyboard shortcuts

#### Scenario: New view with shortcuts implements provider
- **WHEN** a new view with keyboard shortcuts is added to the application
- **THEN** the view SHALL implement `IShortcutHelpProvider` so its shortcuts appear in the help overlay
