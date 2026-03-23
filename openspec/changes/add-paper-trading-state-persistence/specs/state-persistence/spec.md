## ADDED Requirements

### Requirement: Trade records are persisted to SQLite database
The system SHALL persist all trade records to a SQLite database via Entity Framework Core, with each trade stored as a row in the `Trades` table.

#### Scenario: Trade is saved to database after execution
- **WHEN** `ExecutePaperTrade` completes a paper trade
- **THEN** the system SHALL insert a new row into the `Trades` table with the trade's Id, MarketId, Question, Outcome, EntryPrice, Size, Status (`Open`), and CreatedAtUtc
- **AND** the operation SHALL be part of an atomic transaction that also updates the bankroll and writes audit log entries

#### Scenario: Trade status is updated on settlement
- **WHEN** `SettleTrade` completes
- **THEN** the system SHALL update the trade's Status to `Settled` and set `SettledAtUtc` in the `Trades` table
- **AND** the system SHALL insert a new row into the `TradeResults` table with settlement details (SettlementPrice, Payout, ProfitLoss, SettledAtUtc)

#### Scenario: All trades are queryable
- **WHEN** a query is made for open trades
- **THEN** the system SHALL return all trades from the `Trades` table where `Status = 'Open'`
- **WHEN** a query is made for all trades
- **THEN** the system SHALL return all trades from the `Trades` table regardless of status

### Requirement: Bankroll state is persisted to AppState table
The system SHALL persist the current bankroll value in the `AppState` table as a key-value pair with key `"Bankroll"`.

#### Scenario: Bankroll is updated after trade execution
- **WHEN** `ExecutePaperTrade` deducts the trade size from the bankroll
- **THEN** the system SHALL update the `Bankroll` entry in the `AppState` table with the new value and current UTC timestamp

#### Scenario: Bankroll is updated after trade settlement
- **WHEN** `SettleTrade` adds the payout to the bankroll
- **THEN** the system SHALL update the `Bankroll` entry in the `AppState` table with the new value and current UTC timestamp

### Requirement: Repository interfaces defined in Application layer
The system SHALL define repository interfaces in the `Application/Interfaces` namespace following Clean Architecture boundaries.

#### Scenario: ITradeRepository exposes trade persistence methods
- **WHEN** a component needs to persist or query trades
- **THEN** it SHALL use `ITradeRepository` with methods for `GetByIdAsync`, `GetOpenTradesAsync`, `GetAllTradesAsync`, `AddAsync`, and `UpdateAsync`

#### Scenario: IAppStateRepository exposes bankroll persistence methods
- **WHEN** a component needs to read or write the bankroll
- **THEN** it SHALL use `IAppStateRepository` with methods for `GetBankrollAsync` and `SetBankrollAsync`

### Requirement: All mutations occur within database transactions
The system SHALL wrap each state-mutating operation (trade execution, trade settlement) in a database transaction that includes data changes and audit log entries.

#### Scenario: Trade execution is transactional
- **WHEN** `ExecutePaperTrade` is called
- **THEN** the trade insert, bankroll update, and audit log entries SHALL all be committed in a single transaction
- **AND** if any step fails, the entire transaction SHALL be rolled back

#### Scenario: Trade settlement is transactional
- **WHEN** `SettleTrade` is called
- **THEN** the trade update, trade result insert, bankroll update, and audit log entries SHALL all be committed in a single transaction
- **AND** if any step fails, the entire transaction SHALL be rolled back

#### Scenario: Save failures do not crash the application
- **WHEN** a database transaction fails (e.g., disk full, SQLite locked)
- **THEN** the system SHALL log the error, rollback the transaction, and continue operating with in-memory state

### Requirement: Database connection string is configurable
The system SHALL use a connection string from `ConnectionStrings:TradingDb` in appsettings.json for the SQLite database.

#### Scenario: Default connection string
- **WHEN** `ConnectionStrings:TradingDb` is not specified in configuration
- **THEN** the system SHALL use a default value of `"Data Source=data/polyedge-scout.db"`

#### Scenario: Custom connection string
- **WHEN** `ConnectionStrings:TradingDb` is set to a custom value in appsettings.json
- **THEN** the system SHALL use that connection string for the SQLite database

### Requirement: EF Core migrations manage schema evolution
The system SHALL use EF Core Code-First Migrations to create and evolve the database schema.

#### Scenario: Initial migration creates all tables
- **WHEN** the initial migration is applied
- **THEN** the database SHALL contain `Trades`, `TradeResults`, `AuditLog`, and `AppState` tables with correct schemas, primary keys, foreign keys, and indexes

#### Scenario: Migrations are applied on startup
- **WHEN** the application starts
- **THEN** the system SHALL call `Database.MigrateAsync()` to apply any pending migrations before the main application loop begins
