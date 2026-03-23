## ADDED Requirements

### Requirement: State is recovered from SQLite database on startup
The system SHALL query the SQLite database on application startup to restore paper trading state, including open trades and current bankroll.

#### Scenario: Successful state recovery from database
- **WHEN** the application starts and the database contains open trades and a bankroll value
- **THEN** `OrderService` SHALL initialize by querying `ITradeRepository.GetOpenTradesAsync()` to populate `_openTrades`
- **AND** `OrderService` SHALL query `IAppStateRepository.GetBankrollAsync()` to restore `_bankroll`
- **AND** the system SHALL log an informational message indicating state was recovered with the number of open trades and current bankroll

#### Scenario: Successful recovery includes settled trade history
- **WHEN** the application starts and the database contains settled trades
- **THEN** `OrderService` SHALL query `ITradeResultRepository.GetAllAsync()` to populate `_settledTrades`
- **AND** the settled trade history SHALL be available for dashboard display and performance reporting

#### Scenario: Recovery logs an audit entry
- **WHEN** state is successfully recovered from the database
- **THEN** the system SHALL log an `AuditLogEntry` with `Action = Recovered`, `EntityType = "AppState"`, and details of what was recovered (trade counts, bankroll value)

### Requirement: First run initializes default state in database
The system SHALL handle first-run scenarios where the database exists but contains no data.

#### Scenario: First run with empty database
- **WHEN** the application starts and the database has been created (via migrations) but contains no `AppState` entry for `Bankroll`
- **THEN** `OrderService` SHALL initialize with default state (bankroll = 10,000, empty trade lists)
- **AND** the system SHALL insert the default bankroll value into the `AppState` table
- **AND** the system SHALL log an informational message indicating fresh start with default bankroll

#### Scenario: First run creates initial audit entry
- **WHEN** the application initializes with default state for the first time
- **THEN** the system SHALL log an `AuditLogEntry` with `Action = Created`, `EntityType = "AppState"`, `PropertyName = "Bankroll"`, `NewValue = "10000"`

### Requirement: Database creation and migration on startup
The system SHALL create the SQLite database file and apply all migrations automatically on startup.

#### Scenario: Database file does not exist
- **WHEN** the application starts and the SQLite database file does not exist at the configured path
- **THEN** `Database.MigrateAsync()` SHALL create the database file and apply all migrations
- **AND** the system SHALL log that a new database was created

#### Scenario: Database directory does not exist
- **WHEN** the application starts and the directory for the database file does not exist
- **THEN** the system SHALL create the directory before connecting to the database

#### Scenario: Database exists with pending migrations
- **WHEN** the application starts and the database exists but has pending migrations (e.g., after an upgrade)
- **THEN** `Database.MigrateAsync()` SHALL apply only the pending migrations without data loss

### Requirement: Database errors during recovery are handled gracefully
The system SHALL handle database errors during recovery without preventing application startup.

#### Scenario: Corrupted database file
- **WHEN** the application starts and the SQLite database file is corrupted (unreadable)
- **THEN** the system SHALL log a warning with the error details
- **AND** the system SHALL rename the corrupted database file by appending `.corrupted` to preserve it for debugging
- **AND** `OrderService` SHALL initialize with default state and the system SHALL create a fresh database

#### Scenario: Database query fails during recovery
- **WHEN** the application starts and a database query fails during state recovery (e.g., schema mismatch)
- **THEN** the system SHALL log a warning with the error details
- **AND** `OrderService` SHALL initialize with default state
- **AND** the system SHALL NOT delete or modify the existing database
