## 1. Domain Layer: Audit Entities and Enums

- [x] 1.1 Create `AuditAction` enum in `PolyEdgeScout.Domain/Enums/AuditAction.cs` with values: `Created`, `Updated`, `Settled`, `Deleted`, `Recovered`
- [x] 1.2 Create `AuditLogEntry` entity in `PolyEdgeScout.Domain/Entities/AuditLogEntry.cs` with properties: `long Id`, `DateTime Timestamp`, `string EntityType`, `string EntityId`, `AuditAction Action`, `string UserId`, `string? PropertyName`, `string? OldValue`, `string? NewValue`, `string CorrelationId`
- [x] 1.3 Create `AppStateEntry` entity in `PolyEdgeScout.Domain/Entities/AppStateEntry.cs` with properties: `string Key`, `string Value`, `DateTime UpdatedAtUtc`

## 2. Application Layer: Repository Interfaces and DTOs

- [x] 2.1 Create `ITradeRepository` in `PolyEdgeScout.Application/Interfaces/ITradeRepository.cs` with methods: `Task<Trade?> GetByIdAsync(string id)`, `Task<List<Trade>> GetOpenTradesAsync()`, `Task<List<Trade>> GetAllTradesAsync()`, `Task AddAsync(Trade trade)`, `Task UpdateAsync(Trade trade)`
- [x] 2.2 Create `ITradeResultRepository` in `PolyEdgeScout.Application/Interfaces/ITradeResultRepository.cs` with methods: `Task AddAsync(TradeResult result)`, `Task<List<TradeResult>> GetAllAsync()`, `Task<TradeResult?> GetByTradeIdAsync(string tradeId)`
- [x] 2.3 Create `IAuditLogRepository` in `PolyEdgeScout.Application/Interfaces/IAuditLogRepository.cs` with methods: `Task AddAsync(AuditLogEntry entry)`, `Task AddRangeAsync(IEnumerable<AuditLogEntry> entries)`, `Task<List<AuditLogEntry>> GetByEntityAsync(string entityType, string entityId)`, `Task<List<AuditLogEntry>> GetByCorrelationIdAsync(string correlationId)`, `Task<List<AuditLogEntry>> GetByDateRangeAsync(DateTime from, DateTime to)`
- [x] 2.4 Create `IAppStateRepository` in `PolyEdgeScout.Application/Interfaces/IAppStateRepository.cs` with methods: `Task<string?> GetValueAsync(string key)`, `Task SetValueAsync(string key, string value)`, `Task<double> GetBankrollAsync()`, `Task SetBankrollAsync(double bankroll)`
- [x] 2.5 Create `IAuditService` in `PolyEdgeScout.Application/Interfaces/IAuditService.cs` with methods: `Task LogAsync(string entityType, string entityId, AuditAction action, string correlationId, string? propertyName = null, string? oldValue = null, string? newValue = null)`, `Task LogBatchAsync(IEnumerable<AuditLogEntry> entries)`
- [x] 2.6 Create `IUnitOfWork` in `PolyEdgeScout.Application/Interfaces/IUnitOfWork.cs` with methods: `Task<int> SaveChangesAsync(CancellationToken ct = default)`, `Task BeginTransactionAsync()`, `Task CommitTransactionAsync()`, `Task RollbackTransactionAsync()`

## 3. Infrastructure Layer: EF Core Setup

- [x] 3.1 Add NuGet packages to `PolyEdgeScout.Infrastructure.csproj`: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`
- [x] 3.2 Create `TradingDbContext` in `PolyEdgeScout.Infrastructure/Persistence/TradingDbContext.cs` with DbSets for `Trade`, `TradeResult`, `AuditLogEntry`, `AppStateEntry`. Override `OnModelCreating` to apply entity configurations.
- [x] 3.3 Create `TradeConfiguration` in `PolyEdgeScout.Infrastructure/Persistence/Configurations/TradeConfiguration.cs` implementing `IEntityTypeConfiguration<Trade>` — configure primary key, property mappings, indexes (on `Status`, `CreatedAtUtc`)
- [x] 3.4 Create `TradeResultConfiguration` in `PolyEdgeScout.Infrastructure/Persistence/Configurations/TradeResultConfiguration.cs` — configure primary key, foreign key to Trade, property mappings
- [x] 3.5 Create `AuditLogEntryConfiguration` in `PolyEdgeScout.Infrastructure/Persistence/Configurations/AuditLogEntryConfiguration.cs` — configure auto-increment PK, indexes on (`EntityType`, `EntityId`), (`CorrelationId`), (`Timestamp`), store `Action` as string, store `Timestamp` as ISO 8601 text
- [x] 3.6 Create `AppStateEntryConfiguration` in `PolyEdgeScout.Infrastructure/Persistence/Configurations/AppStateEntryConfiguration.cs` — configure `Key` as primary key

## 4. Infrastructure Layer: Repository Implementations

- [x] 4.1 Create `TradeRepository` in `PolyEdgeScout.Infrastructure/Persistence/Repositories/TradeRepository.cs` implementing `ITradeRepository` using `TradingDbContext`
- [x] 4.2 Create `TradeResultRepository` in `PolyEdgeScout.Infrastructure/Persistence/Repositories/TradeResultRepository.cs` implementing `ITradeResultRepository`
- [x] 4.3 Create `AuditLogRepository` in `PolyEdgeScout.Infrastructure/Persistence/Repositories/AuditLogRepository.cs` implementing `IAuditLogRepository` — only `AddAsync`, `AddRangeAsync`, and query methods (no update/delete methods)
- [x] 4.4 Create `AppStateRepository` in `PolyEdgeScout.Infrastructure/Persistence/Repositories/AppStateRepository.cs` implementing `IAppStateRepository`
- [x] 4.5 Create `UnitOfWork` in `PolyEdgeScout.Infrastructure/Persistence/UnitOfWork.cs` implementing `IUnitOfWork`, wrapping `TradingDbContext` transaction management

## 5. Infrastructure Layer: Audit Service

- [x] 5.1 Create `AuditService` in `PolyEdgeScout.Infrastructure/Services/AuditService.cs` implementing `IAuditService` — constructs `AuditLogEntry` with UTC ISO 8601 timestamp, `UserId = "system"`, and delegates to `IAuditLogRepository`

## 6. Configuration and DI Registration

- [x] 6.1 Add `DatabaseConnectionString` property to `AppConfig` (or use standard `ConnectionStrings` pattern) with default `"Data Source=data/polyedge-scout.db"`
- [x] 6.2 Add `"ConnectionStrings": { "TradingDb": "Data Source=data/polyedge-scout.db" }` to `src/PolyEdgeScout.Console/appsettings.json`
- [x] 6.3 Update `ServiceCollectionExtensions.cs` in Infrastructure/DependencyInjection to register: `TradingDbContext` with SQLite provider, all repository implementations, `AuditService`, `UnitOfWork`
- [x] 6.4 Add `dotnet ef` tool reference to solution (global or local tool manifest)

## 7. EF Core Migrations

- [x] 7.1 Create initial EF Core migration (`InitialCreate`) that creates `Trades`, `TradeResults`, `AuditLog`, and `AppState` tables with all indexes and constraints
- [x] 7.2 Add startup code in `Program.cs` to call `context.Database.MigrateAsync()` to auto-apply pending migrations

## 8. Integrate Persistence into OrderService

- [x] 8.1 Replace `IStateStore` dependency in `OrderService` with `ITradeRepository`, `ITradeResultRepository`, `IAppStateRepository`, `IAuditService`, and `IUnitOfWork`
- [x] 8.2 Add `InitializeAsync` method that queries open trades from `ITradeRepository.GetOpenTradesAsync()` and bankroll from `IAppStateRepository.GetBankrollAsync()`, populating in-memory state
- [x] 8.3 Update `ExecutePaperTrade` to: begin transaction → save trade via `ITradeRepository.AddAsync` → update bankroll via `IAppStateRepository.SetBankrollAsync` → log audit entries (trade created + bankroll updated) with shared correlation ID → commit transaction
- [x] 8.4 Update `SettleTrade` to: begin transaction → update trade status via `ITradeRepository.UpdateAsync` → save trade result via `ITradeResultRepository.AddAsync` → update bankroll via `IAppStateRepository.SetBankrollAsync` → log audit entries (trade settled + bankroll updated) with shared correlation ID → commit transaction
- [x] 8.5 Add error handling: wrap transaction operations in try/catch, rollback on failure, log errors, continue with in-memory state

## 9. Startup Integration

- [x] 9.1 Update `Program.cs` to run EF Core migrations on startup (`MigrateAsync`)
- [x] 9.2 Call `OrderService.InitializeAsync` after DI container is built and database is migrated
- [x] 9.3 Ensure graceful handling: log recovery success with trade counts and bankroll, or log fresh-start message
- [x] 9.4 Seed initial `AppState` bankroll entry (10,000) if not present in database

## 10. Unit Tests

- [x] 10.1 Create `TradeRepositoryTests` in `PolyEdgeScout.Infrastructure.Tests` — test CRUD operations with in-memory SQLite
- [x] 10.2 Create `AuditLogRepositoryTests` in `PolyEdgeScout.Infrastructure.Tests` — test add and query operations, verify no update/delete methods exist
- [x] 10.3 Create `AppStateRepositoryTests` in `PolyEdgeScout.Infrastructure.Tests` — test get/set bankroll, key-value operations
- [x] 10.4 Create `AuditServiceTests` in `PolyEdgeScout.Application.Tests` — test audit entries are created with correct timestamps, entity types, correlation IDs
- [x] 10.5 N/A `OrderServicePersistenceTests` in `PolyEdgeScout.Application.Tests` — test InitializeAsync loads state, ExecuteTrade creates trade + audit entries in transaction, SettleTrade creates result + audit entries in transaction, transaction rollback on failure
- [x] 10.6 Create `AuditTrailIntegrityTests` in `PolyEdgeScout.Infrastructure.Tests` — test audit log is append-only (no update/delete via repository), test correlation IDs group related entries, test ISO 8601 timestamp format
- [x] 10.7 Verify all existing tests still pass after modifications

## 11. Remove Old IStateStore (Cleanup)

- [x] 11.1 N/A — `IStateStore` never existed (was from old JSON proposal)
- [x] 11.2 N/A — `JsonStateStore` never existed
- [x] 11.3 N/A — `TradingState` DTO never existed
- [x] 11.4 N/A — `StateFilePath` never existed
- [x] 11.5 N/A — `IStateStore` binding never existed
