## ADDED Requirements

### Requirement: Domain layer has no project dependencies
The Domain assembly (`PolyEdgeScout.Domain`) SHALL have zero references to any other PolyEdgeScout assembly. It is the innermost layer and depends only on .NET SDK types.

#### Scenario: Domain assembly does not reference Application
- **WHEN** the referenced assemblies of `PolyEdgeScout.Domain` are inspected
- **THEN** none of the referenced assembly names SHALL contain `PolyEdgeScout.Application`

#### Scenario: Domain assembly does not reference Infrastructure
- **WHEN** the referenced assemblies of `PolyEdgeScout.Domain` are inspected
- **THEN** none of the referenced assembly names SHALL contain `PolyEdgeScout.Infrastructure`

#### Scenario: Domain assembly does not reference Console
- **WHEN** the referenced assemblies of `PolyEdgeScout.Domain` are inspected
- **THEN** none of the referenced assembly names SHALL contain `PolyEdgeScout.Console`

---

### Requirement: Application layer depends only on Domain
The Application assembly (`PolyEdgeScout.Application`) SHALL reference only `PolyEdgeScout.Domain` among PolyEdgeScout assemblies. It SHALL NOT reference Infrastructure or Console.

#### Scenario: Application references Domain
- **WHEN** the referenced assemblies of `PolyEdgeScout.Application` are inspected
- **THEN** the referenced assembly names SHALL include `PolyEdgeScout.Domain`

#### Scenario: Application does not reference Infrastructure
- **WHEN** the referenced assemblies of `PolyEdgeScout.Application` are inspected
- **THEN** none of the referenced assembly names SHALL contain `PolyEdgeScout.Infrastructure`

#### Scenario: Application does not reference Console
- **WHEN** the referenced assemblies of `PolyEdgeScout.Application` are inspected
- **THEN** none of the referenced assembly names SHALL contain `PolyEdgeScout.Console`

---

### Requirement: Infrastructure depends on Domain and Application only
The Infrastructure assembly (`PolyEdgeScout.Infrastructure`) SHALL reference only `PolyEdgeScout.Domain` and `PolyEdgeScout.Application` among PolyEdgeScout assemblies. It SHALL NOT reference Console.

#### Scenario: Infrastructure references Domain
- **WHEN** the referenced assemblies of `PolyEdgeScout.Infrastructure` are inspected
- **THEN** the referenced assembly names SHALL include `PolyEdgeScout.Domain`

#### Scenario: Infrastructure references Application
- **WHEN** the referenced assemblies of `PolyEdgeScout.Infrastructure` are inspected
- **THEN** the referenced assembly names SHALL include `PolyEdgeScout.Application`

#### Scenario: Infrastructure does not reference Console
- **WHEN** the referenced assemblies of `PolyEdgeScout.Infrastructure` are inspected
- **THEN** none of the referenced assembly names SHALL contain `PolyEdgeScout.Console`

---

### Requirement: Console is the composition root
The Console assembly (`PolyEdgeScout.Console`) is the entry point and MAY reference all other PolyEdgeScout assemblies.

#### Scenario: Console references Application
- **WHEN** the referenced assemblies of `PolyEdgeScout.Console` are inspected
- **THEN** the referenced assembly names SHALL include `PolyEdgeScout.Application`

#### Scenario: Console references Infrastructure
- **WHEN** the referenced assemblies of `PolyEdgeScout.Console` are inspected
- **THEN** the referenced assembly names SHALL include `PolyEdgeScout.Infrastructure`

---

### Requirement: Domain entities are sealed records
All public types in the `PolyEdgeScout.Domain.Entities` namespace SHALL be `sealed record` types, enforcing immutability as the project convention.

#### Scenario: All entity types are sealed
- **WHEN** all public types in the `PolyEdgeScout.Domain.Entities` namespace are inspected
- **THEN** every type SHALL have `IsSealed == true`

#### Scenario: All entity types are records
- **WHEN** all public types in the `PolyEdgeScout.Domain.Entities` namespace are inspected
- **THEN** every type SHALL be a C# record (detected by the presence of the compiler-generated `EqualityContract` property)

#### Scenario: At least one entity exists
- **WHEN** the `PolyEdgeScout.Domain.Entities` namespace is inspected
- **THEN** there SHALL be at least one public type (guard against false positives from an empty namespace)

---

### Requirement: Repository and service interfaces are in Application.Interfaces
All interfaces whose names end in "Repository" or "Service" that are part of the application's contract SHALL reside in the `PolyEdgeScout.Application.Interfaces` namespace.

#### Scenario: All repository interfaces are in Application.Interfaces
- **WHEN** all public interfaces across all PolyEdgeScout assemblies whose names end in "Repository" are collected
- **THEN** each interface in `PolyEdgeScout.Application` SHALL be in the `PolyEdgeScout.Application.Interfaces` namespace

#### Scenario: All service interfaces in Application are in Application.Interfaces
- **WHEN** all public interfaces in `PolyEdgeScout.Application` whose names match the pattern `I*Service` are collected
- **THEN** each SHALL be in the `PolyEdgeScout.Application.Interfaces` namespace

#### Scenario: Domain interfaces are allowed in Domain.Interfaces
- **WHEN** public interfaces in `PolyEdgeScout.Domain` are inspected
- **THEN** they SHALL be in the `PolyEdgeScout.Domain.Interfaces` namespace (this is permitted — Domain owns its own contracts like `ILogService`)

---

### Requirement: No circular dependencies between layers
The layer dependency graph SHALL form a directed acyclic graph (DAG). No circular references SHALL exist between any PolyEdgeScout assemblies.

#### Scenario: Layer dependencies form a DAG
- **WHEN** the layer dependency tests (Rules 1-4) all pass
- **THEN** the dependency graph is implicitly acyclic — Domain→nothing, Application→Domain, Infrastructure→Domain+Application, Console→all
- **AND** no additional circular dependency check is needed because the strict layer rules prevent cycles

---

### Requirement: Audit log repository is append-only
The `IAuditLogRepository` interface SHALL only expose methods for adding and querying audit entries. It SHALL NOT expose any method that modifies or deletes existing entries.

#### Scenario: No Update methods on IAuditLogRepository
- **WHEN** the methods of `IAuditLogRepository` are inspected via reflection
- **THEN** no method name SHALL contain "Update"

#### Scenario: No Delete methods on IAuditLogRepository
- **WHEN** the methods of `IAuditLogRepository` are inspected via reflection
- **THEN** no method name SHALL contain "Delete"

#### Scenario: No Remove methods on IAuditLogRepository
- **WHEN** the methods of `IAuditLogRepository` are inspected via reflection
- **THEN** no method name SHALL contain "Remove"

#### Scenario: Only Add and query methods are present
- **WHEN** the methods declared on `IAuditLogRepository` (excluding inherited `object` methods) are listed
- **THEN** the methods SHALL be: `AddAsync`, `AddRangeAsync`, `GetByEntityAsync`, `GetByCorrelationIdAsync`, `GetByDateRangeAsync` (or a subset thereof — new query methods are allowed, but mutation methods are not)

---

### Requirement: Naming conventions are followed
Repository interfaces SHALL end in "Repository", service interfaces SHALL end in "Service", and their implementations SHALL follow the same naming pattern without the "I" prefix.

#### Scenario: Repository interfaces end in "Repository"
- **WHEN** all public interfaces in `PolyEdgeScout.Application.Interfaces` that conceptually represent repositories are inspected
- **THEN** each SHALL have a name matching the pattern `I*Repository`

#### Scenario: Service interfaces end in "Service"
- **WHEN** all public interfaces in `PolyEdgeScout.Application.Interfaces` that represent services are inspected
- **THEN** each SHALL have a name matching the pattern `I*Service`

#### Scenario: Enum types are in the Enums namespace
- **WHEN** all public enum types in `PolyEdgeScout.Domain` are inspected
- **THEN** each SHALL be in the `PolyEdgeScout.Domain.Enums` namespace

---

### Requirement: All domain entities are in the Entities namespace
All entity types (non-enum, non-interface, non-value-object public types that represent domain concepts) in the Domain assembly SHALL reside in the `PolyEdgeScout.Domain.Entities` namespace.

#### Scenario: Entity types are in Domain.Entities
- **WHEN** all public record types in `PolyEdgeScout.Domain` that are in the `Entities` namespace are inspected
- **THEN** they SHALL include known entities: `Trade`, `TradeResult`, `AuditLogEntry`, `Market`, `PnlSnapshot`, `AppStateEntry`

#### Scenario: No entity types exist outside Domain.Entities
- **WHEN** all public sealed record types in `PolyEdgeScout.Domain` are inspected
- **THEN** each SHALL be in the `PolyEdgeScout.Domain.Entities` namespace (i.e., no sealed records scattered in other Domain namespaces that should be entities)
