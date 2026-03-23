## Why

The PolyEdgeScout paper trading system currently stores all state — open trades, settled trades, and bankroll — in-memory within `OrderService`. When the application restarts (crash, manual restart, deployment), **all paper trading data is lost**. This makes it impossible to:

- Track paper trading performance across sessions
- Resume monitoring of open positions after a restart
- Build confidence in the system's edge before switching to live trading
- Run paper trading over multi-day periods (the primary use case)
- Audit trade decisions and state transitions for quality assurance
- Maintain a tamper-evident history of all system actions

State persistence with a proper audit trail is a prerequisite for any serious paper trading evaluation and operational confidence.

## What Changes

Add a SQLite-backed persistence layer using Entity Framework Core that stores paper trading state in a relational database with full ISO-compliant audit trails. The design follows Clean Architecture: repository interfaces in Application, EF Core DbContext and implementations in Infrastructure, and integration into `OrderService` to save/load automatically. Every state mutation (trade creation, settlement, bankroll changes) is recorded in an immutable, append-only audit log following ISO 27001 (information security) and ISO 9001 (quality management) audit trail requirements.

## Capabilities

### New Capabilities
- `state-persistence`: Persist paper trading state (bankroll, open trades, settled trades) to a local SQLite database via EF Core, with queryable trade histories and structured relational storage
- `state-recovery`: Load persisted state from the SQLite database on application startup, restoring open trades and bankroll from the last known good state with graceful fallback to defaults
- `audit-trails`: Record all state mutations in an immutable, append-only audit log with ISO 8601 UTC timestamps, entity tracking, before/after values, action types, and correlation IDs — compliant with ISO 27001 and ISO 9001 audit trail requirements

### Modified Capabilities
<!-- None — no existing spec-level behavior changes -->

## Impact

- **`PolyEdgeScout.Domain`** — New `AuditLogEntry` entity, `AuditAction` enum
- **`PolyEdgeScout.Application`** — New repository interfaces (`ITradeRepository`, `IAuditLogRepository`, `IAppStateRepository`), new `AuditLogEntry` and `AppState` DTOs, `OrderService` modified to use repositories and audit service
- **`PolyEdgeScout.Infrastructure`** — New EF Core `TradingDbContext`, entity configurations, repository implementations, `AuditInterceptor`/`AuditService`, SQLite provider setup, migrations
- **`PolyEdgeScout.Application/Configuration/AppConfig.cs`** — New `DatabaseConnectionString` property
- **`appsettings.json`** — New `ConnectionStrings:TradingDb` config entry
- **`PolyEdgeScout.Console/Program.cs`** — Database initialization, migration on startup
- **`PolyEdgeScout.Infrastructure/DependencyInjection/`** — Register DbContext, repositories, audit service
- **`PolyEdgeScout.Application.Tests`** — New unit tests for repositories, audit logging, and OrderService persistence behavior
- **`PolyEdgeScout.Infrastructure.Tests`** — Integration tests for EF Core, SQLite, and audit trail integrity

### NuGet Packages
- `Microsoft.EntityFrameworkCore.Sqlite` — SQLite database provider for EF Core
- `Microsoft.EntityFrameworkCore.Design` — EF Core migrations tooling (design-time)
- `Microsoft.EntityFrameworkCore.Tools` — EF Core CLI tools for migrations
