## ADDED Requirements

### Requirement: The application supports switching between Dashboard and Error Log views
The application SHALL provide a navigation mechanism to switch between the Dashboard view and the Error Log view. Only one view SHALL be active (visible) at a time.

#### Scenario: Application starts on the Dashboard view
- **WHEN** the application launches
- **THEN** the Dashboard view SHALL be the active view
- **AND** the Error Log view SHALL NOT be visible

#### Scenario: Switching to Error Log view hides Dashboard
- **WHEN** the user navigates to the Error Log view
- **THEN** the Dashboard content SHALL be hidden
- **AND** the Error Log content SHALL be visible and focused
- **AND** the MenuBar and StatusBar SHALL remain visible and functional

#### Scenario: Switching back to Dashboard view hides Error Log
- **WHEN** the user navigates back to the Dashboard view from the Error Log view
- **THEN** the Error Log content SHALL be hidden
- **AND** the Dashboard content SHALL be visible with its previous state intact
- **AND** focus SHALL return to the Dashboard's previously focused child view

#### Scenario: Background scan loop continues during view switches
- **WHEN** the user switches between views
- **THEN** the background scan loop SHALL continue running without interruption
- **AND** the Dashboard ViewModels SHALL continue receiving updates even when the Dashboard is not visible
- **AND** the Error Log ViewModel SHALL continue receiving new entries even when the Error Log is not visible

### Requirement: Raw console output does not bleed through the Terminal.Gui surface
The logging infrastructure SHALL NOT write directly to `Console.WriteLine` or any standard output stream while Terminal.Gui owns the console surface.

#### Scenario: FileLogService does not write to Console.WriteLine
- **WHEN** any code path calls `ILogService.Info`, `Warn`, `Error`, or `Debug`
- **THEN** the `FileLogService` SHALL write to the log file
- **AND** the `FileLogService` SHALL raise a `LogEntryWritten` event with the level and formatted message
- **AND** the `FileLogService` SHALL NOT call `Console.WriteLine` or `Console.Write`

#### Scenario: Log entries are routed to TUI views via events
- **WHEN** `FileLogService` raises a `LogEntryWritten` event
- **THEN** the Console layer SHALL receive the event via a subscribed handler
- **AND** the handler SHALL route `ERR` and `WRN` entries to `ErrorLogViewModel`
- **AND** no raw text SHALL appear on the Terminal.Gui surface
