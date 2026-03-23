## Context

PolyEdgeScout follows .NET Clean Architecture with four layers:

| Layer | Assembly | References |
|---|---|---|
| **Domain** | `PolyEdgeScout.Domain` | None (zero project references) |
| **Application** | `PolyEdgeScout.Application` | Domain only |
| **Infrastructure** | `PolyEdgeScout.Infrastructure` | Domain, Application |
| **Console** | `PolyEdgeScout.Console` | Domain, Application, Infrastructure |

Key structural conventions already in place:
- Domain entities are `sealed record` types (e.g., `Trade`, `TradeResult`, `AuditLogEntry`, `Market`, `PnlSnapshot`, `AppStateEntry`)
- All repository/service interfaces live in `PolyEdgeScout.Application.Interfaces`
- `IAuditLogRepository` is deliberately append-only (only `AddAsync`, `AddRangeAsync`, and query methods)
- Domain has a single interface (`ILogService`) in `PolyEdgeScout.Domain.Interfaces`
- The project targets .NET 10.0

## Goals / Non-Goals

**Goals:**
- Verify layer dependency rules at test time using assembly metadata and reflection
- Verify naming conventions for interfaces and implementations
- Verify domain entity immutability (`sealed record`)
- Verify `IAuditLogRepository` append-only constraint
- Verify interface placement in the correct namespaces
- Keep tests fast (<1 second total) — no I/O, no database, pure reflection
- Use plain xUnit + reflection (no third-party architecture testing libraries)
- Tests serve as living architecture documentation

**Non-Goals:**
- Runtime enforcement of architecture rules (these are test-time only)
- Full dependency injection verification (testing that all services are registered is out of initial scope)
- Verifying NuGet package usage policies (e.g., "Domain must not use EF Core packages")
- Code complexity or static analysis metrics
- Enforcing coding style (that's the linter's job)

## Decisions

### 1. Plain reflection + xUnit over NetArchTest
**Decision:** Use .NET reflection APIs with xUnit assertions instead of the NetArchTest NuGet package.
**Rationale:** The architecture rules being tested are simple enough to express with `Assembly.GetReferencedAssemblies()`, `Type.IsSealed`, `Type.GetMethods()`, etc. Adding NetArchTest would introduce a third-party dependency for ~10 tests that are straightforward with reflection. The team can always migrate to NetArchTest later if the rules become more complex. Keeping dependencies minimal aligns with the project's philosophy.

### 2. Dedicated test project `PolyEdgeScout.Architecture.Tests`
**Decision:** Create a new test project rather than adding tests to an existing test project.
**Rationale:** Architecture tests are cross-cutting — they inspect all four assemblies. Placing them in `PolyEdgeScout.Application.Tests` or `PolyEdgeScout.Domain.Tests` would be misleading. A dedicated project clearly signals these are structural verification tests, and it references all four assemblies to inspect them.

### 3. Assembly reference inspection via `GetReferencedAssemblies()`
**Decision:** Use `Assembly.GetReferencedAssemblies()` to verify which assemblies each layer depends on.
**Rationale:** This checks the actual compiled assembly metadata, not the `.csproj` file. It reflects the true runtime dependency graph. If someone adds a transitive reference that violates the rules, the test catches it.

### 4. Entity immutability via type reflection
**Decision:** Verify `sealed record` by checking `Type.IsSealed` and confirming the type is a record (has `<Clone>$` method or `EqualityContract` property, which are compiler-generated for records).
**Rationale:** C# records generate specific compiler artifacts. Checking for `IsSealed` plus the presence of `EqualityContract` (a protected virtual property on records) reliably detects `sealed record` types.

### 5. Append-only constraint via interface method inspection
**Decision:** Use `typeof(IAuditLogRepository).GetMethods()` to verify no method names contain "Update", "Delete", or "Remove".
**Rationale:** This is a direct reflection of the design decision documented in the state persistence proposal. The test ensures the interface contract stays append-only even as the codebase evolves.

### 6. Test organization: one test class per architectural concern
**Decision:** Organize tests into separate classes: `LayerDependencyTests`, `NamingConventionTests`, `EntityImmutabilityTests`, `InterfacePlacementTests`, `AuditLogConstraintTests`, `DomainIsolationTests`.
**Rationale:** Each class has a clear responsibility and can be run independently. Failures point directly to the violated architectural concern.

### 7. No circular dependency check via separate test
**Decision:** The layer dependency tests implicitly prevent circular dependencies. If Domain references Application (violating Rule 1), the test fails. No separate circular dependency detection algorithm is needed.
**Rationale:** In a strict layered architecture, the layer tests already enforce a DAG (directed acyclic graph). A circular dependency would require a lower layer to reference a higher one, which the existing tests catch.

## Test Architecture

```
tests/PolyEdgeScout.Architecture.Tests/
├── PolyEdgeScout.Architecture.Tests.csproj   (references all 4 src projects)
├── Helpers/
│   └── AssemblyHelper.cs                      (shared reflection utilities)
├── LayerDependencyTests.cs                    (Rules 1-4, 7)
├── NamingConventionTests.cs                   (Rule 9)
├── EntityImmutabilityTests.cs                 (Rule 5)
├── InterfacePlacementTests.cs                 (Rule 6)
├── AuditLogConstraintTests.cs                 (Rule 8)
├── DomainIsolationTests.cs                    (Rule 10 + Domain purity)
└── DomainEntityNamespaceTests.cs              (Rule 10)
```

## Assembly References for Test Project

The architecture test project must reference all four source assemblies to inspect them:

```xml
<ProjectReference Include="..\..\src\PolyEdgeScout.Domain\PolyEdgeScout.Domain.csproj" />
<ProjectReference Include="..\..\src\PolyEdgeScout.Application\PolyEdgeScout.Application.csproj" />
<ProjectReference Include="..\..\src\PolyEdgeScout.Infrastructure\PolyEdgeScout.Infrastructure.csproj" />
<ProjectReference Include="..\..\src\PolyEdgeScout.Console\PolyEdgeScout.Console.csproj" />
```

This does **not** violate architecture rules — the test project is not part of the production dependency graph.
