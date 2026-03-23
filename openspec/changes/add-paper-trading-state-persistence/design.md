## Context

`OrderService` is the central trade management service. It holds three pieces of mutable state behind a `lock`:
- `_openTrades` (List\<Trade\>) — currently active paper trades
- `_settledTrades` (List\<TradeResult\>) — completed trades with P&L
- `_bankroll` (double) — current paper bankroll (starts at 10,000)

All three are lost on process exit. The `Trade` and `TradeResult` entities are `sealed record` types (immutable, serializable). The project follows .NET Clean Architecture (Domain → Application → Infrastructure → Console).

## Goals / Non-Goals

**Goals:**
- Persist paper trading state to a local SQLite database after every state mutation
- Recover state on startup, resuming exactly where the previous session left off
- Record every state mutation in an immutable, append-only audit log (ISO 27001 / ISO 9001 compliant)
- Provide queryable trade history with structured relational storage
- Use EF Core with SQLite provider for database access, migrations, and schema evolution
- Maintain Clean Architecture boundaries (interfaces in Application, EF Core in Infrastructure)
- Ensure thread-safety is preserved (OrderService already uses `lock`)
- Support schema migrations for future evolution

**Non-Goals:**
- Cloud database or remote storage (local SQLite is sufficient for single-user)
- Live trading state persistence (live trades should not rely on local recovery)
- Multi-user or multi-instance access patterns
- Real-time replication or external audit system integration
- Full SIEM integration (audit log is queryable locally, not shipped to external systems)
- Cryptographic signing of audit records (out of scope; tamper evidence via append-only + no deletes)
- Backup/rotation of database files (can be added later)

## Decisions

### 1. SQLite as storage engine via EF Core
**Decision:** Use Entity Framework Core with the `Microsoft.EntityFrameworkCore.Sqlite` provider for all persistence.
**Rationale:** SQLite provides ACID transactions, queryable relational storage, and zero deployment overhead (single file). EF Core provides migrations for schema evolution, LINQ queries, and change tracking. This is a significant upgrade from a flat JSON file — it supports complex queries on trade history, proper indexing, and relational integrity.

### 2. Database schema: four core tables
**Decision:** Define four tables: `Trades`, `TradeResults`, `AppState`, and `AuditLog`.
**Rationale:**
- **Trades** — stores all trade records (both open and settled), with a `Status` column to distinguish them
- **TradeResults** — stores settlement outcomes linked to trades, with P&L and settlement details
- **AppState** — key-value store for application-level state (bankroll, last session info)
- **AuditLog** — immutable, append-only log of every state mutation

### 3. Immutable, append-only AuditLog table (ISO 27001 / ISO 9001)
**Decision:** The `AuditLog` table is append-only. No UPDATE or DELETE operations are ever performed on it. The application layer enforces this by only exposing an `AddAsync` method.
**Schema:**
| Column | Type | Description |
|---|---|---|
| `Id` | `long` (auto-increment) | Primary key |
| `Timestamp` | `TEXT` (ISO 8601 UTC) | When the action occurred, e.g. `2026-03-23T14:30:00.000Z` |
| `EntityType` | `TEXT` | Type of entity affected: `Trade`, `TradeResult`, `AppState` |
| `EntityId` | `TEXT` | Identifier of the affected entity |
| `Action` | `TEXT` | Action type: `Created`, `Updated`, `Settled`, `Deleted`, `Recovered` |
| `UserId` | `TEXT` | Actor identifier (always `"system"` for paper trading) |
| `PropertyName` | `TEXT` (nullable) | Specific property changed (for updates), e.g. `Bankroll`, `Status` |
| `OldValue` | `TEXT` (nullable) | Previous value (serialized as string) |
| `NewValue` | `TEXT` (nullable) | New value (serialized as string) |
| `CorrelationId` | `TEXT` | Groups related audit entries (e.g., a trade + bankroll change from one execution) |

**ISO Compliance:**
- **ISO 27001 (A.12.4.1 — Event logging):** All state changes are logged with timestamps, actors, and details. The append-only constraint provides tamper evidence.
- **ISO 27001 (A.12.4.2 — Protection of log information):** Audit records cannot be modified or deleted through the application layer. The repository interface only exposes `AddAsync` and query methods.
- **ISO 9001 (7.5 — Documented information):** Full traceability of every decision and state change. Before/after values enable reconstruction of system state at any point.
- **ISO 9001 (10.2 — Nonconformity and corrective action):** Audit trail enables root cause analysis of unexpected trading outcomes.

### 4. Repository pattern with interfaces in Application layer
**Decision:** Define `ITradeRepository`, `ITradeResultRepository`, `IAuditLogRepository`, and `IAppStateRepository` interfaces in `Application/Interfaces`. Implementations use EF Core in Infrastructure.
**Rationale:** Follows Clean Architecture. `OrderService` depends only on abstractions. Enables unit testing with in-memory fakes and future replacement with other storage backends.

### 5. EF Core DbContext in Infrastructure layer
**Decision:** Create `TradingDbContext` in `Infrastructure/Persistence/` that configures all entity mappings and relationships.
**Rationale:** Keeps EF Core concerns in Infrastructure. The DbContext is registered as a scoped service. Entity configurations use the Fluent API in separate `IEntityTypeConfiguration<T>` classes.

### 6. Audit service / interceptor pattern
**Decision:** Create an `IAuditService` interface in Application with an `AuditService` implementation that writes to `IAuditLogRepository`. `OrderService` calls `IAuditService` explicitly after each mutation within the same transaction scope.
**Rationale:** Explicit audit calls are more transparent than EF Core SaveChanges interceptors for our use case. Each business operation creates a correlation ID and logs all related changes atomically.

### 7. Save after every mutation within a transaction
**Decision:** Each state mutation (execute trade, settle trade) opens a transaction, performs the data changes, writes audit log entries, and commits atomically.
**Rationale:** Ensures audit log and data are always in sync. If the audit write fails, the data change is also rolled back. This is critical for ISO compliance — you cannot have a state change without a corresponding audit record.

### 8. Connection string in appsettings.json
**Decision:** Store the SQLite connection string in `ConnectionStrings:TradingDb` in appsettings.json. Default: `"Data Source=data/polyedge-scout.db"`.
**Rationale:** Standard .NET configuration pattern. The `data/` directory keeps the database file organized alongside the application.

### 9. Migrations for schema evolution
**Decision:** Use EF Core Code-First Migrations. The initial migration creates all four tables. Future schema changes are handled by adding new migrations.
**Rationale:** Migrations provide a versioned, repeatable schema evolution path. On startup, `context.Database.MigrateAsync()` applies any pending migrations.

### 10. State recovery from database queries
**Decision:** On startup, recovery queries open trades (`WHERE Status = 'Open'`) and reads the current bankroll from the `AppState` table. No snapshot file needed.
**Rationale:** The database is the single source of truth. Recovery is a simple query, not a deserialization of a snapshot. This is more resilient than file-based recovery.

## Database Schema Diagram

```
┌──────────────────────┐     ┌──────────────────────────┐
│       Trades         │     │      TradeResults         │
├──────────────────────┤     ├──────────────────────────┤
│ Id (PK, TEXT)        │────→│ Id (PK, TEXT)            │
│ MarketId             │     │ TradeId (FK → Trades)    │
│ Question             │     │ SettlementPrice          │
│ Outcome              │     │ Payout                   │
│ EntryPrice           │     │ ProfitLoss               │
│ Size                 │     │ SettledAtUtc             │
│ Status               │     └──────────────────────────┘
│ CreatedAtUtc         │
│ SettledAtUtc (null)  │
└──────────────────────┘
                              ┌──────────────────────────┐
┌──────────────────────┐     │       AuditLog            │
│      AppState        │     ├──────────────────────────┤
├──────────────────────┤     │ Id (PK, BIGINT)          │
│ Key (PK, TEXT)       │     │ Timestamp (TEXT, ISO8601) │
│ Value (TEXT)         │     │ EntityType (TEXT)         │
│ UpdatedAtUtc (TEXT)  │     │ EntityId (TEXT)           │
└──────────────────────┘     │ Action (TEXT)             │
                              │ UserId (TEXT)             │
                              │ PropertyName (TEXT, null) │
                              │ OldValue (TEXT, null)     │
                              │ NewValue (TEXT, null)     │
                              │ CorrelationId (TEXT)      │
                              └──────────────────────────┘
```

## Risks / Trade-offs

- **EF Core + SQLite adds a dependency** — Acceptable trade-off for queryable storage, migrations, and audit trail support. SQLite has zero deployment overhead.
- **Write-after-every-mutation adds I/O on the hot path** — Acceptable for paper trading (not latency-sensitive). SQLite WAL mode can be enabled for better concurrent read/write performance.
- **No cryptographic signing of audit records** — Append-only enforcement is application-level, not cryptographic. Sufficient for single-user paper trading; could add hash chains later for stronger tamper evidence.
- **Database file corruption** — SQLite is resilient to most failure modes, but catastrophic disk failure could lose data. Mitigated by SQLite's journal/WAL mode. Backup support can be added later.
- **EF Core overhead for simple operations** — EF Core's change tracker adds some overhead. Acceptable for the low throughput of paper trading operations.
- **Migration complexity** — EF Core migrations require tooling (`dotnet ef`). Mitigated by auto-migration on startup for this single-user tool.
