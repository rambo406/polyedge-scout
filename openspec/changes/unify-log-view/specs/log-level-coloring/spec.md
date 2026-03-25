## ADDED Requirements

### Requirement: ERR entries display with red foreground
Log entries with level `[ERR]` SHALL be rendered with a red foreground color in the full log view's list mode.

#### Scenario: Error entry appears in red
- **WHEN** a log entry with level ERR is displayed in the full log view
- **THEN** the entry text SHALL be rendered with a red foreground color attribute

### Requirement: WRN entries display with yellow foreground
Log entries with level `[WRN]` SHALL be rendered with a yellow foreground color in the full log view's list mode.

#### Scenario: Warning entry appears in yellow
- **WHEN** a log entry with level WRN is displayed in the full log view
- **THEN** the entry text SHALL be rendered with a yellow foreground color attribute

### Requirement: INF entries display with default foreground
Log entries with level `[INF]` SHALL be rendered with the default foreground color in the full log view.

#### Scenario: Info entry appears in default color
- **WHEN** a log entry with level INF is displayed in the full log view
- **THEN** the entry text SHALL be rendered with the default terminal foreground color

### Requirement: DBG entries display with gray foreground
Log entries with level `[DBG]` SHALL be rendered with a gray or dim foreground color in the full log view's list mode.

#### Scenario: Debug entry appears in gray
- **WHEN** a log entry with level DBG is displayed in the full log view
- **THEN** the entry text SHALL be rendered with a gray or dim foreground color attribute

### Requirement: Color coding degrades gracefully
On terminals that do not support color, the full log view SHALL still display log entries with their `[LEVEL]` text prefix, ensuring readability without color.

#### Scenario: Non-color terminal fallback
- **WHEN** the terminal does not support color rendering
- **THEN** log entries SHALL still be readable with their `[ERR]`, `[WRN]`, `[INF]`, `[DBG]` text prefixes
