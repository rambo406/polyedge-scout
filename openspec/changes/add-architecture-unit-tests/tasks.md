## 1. Project Setup

- [x] 1.1 Create `tests/PolyEdgeScout.Architecture.Tests/PolyEdgeScout.Architecture.Tests.csproj` with:
  - Target framework `net10.0`
  - Project references to all four source assemblies (`Domain`, `Application`, `Infrastructure`, `Console`)
  - Package references: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`
- [x] 1.2 Add the new test project to `PolyEdgeScout.slnx`
- [x] 1.3 Create `tests/PolyEdgeScout.Architecture.Tests/Helpers/AssemblyHelper.cs` with shared reflection utilities:
  - `GetDomainAssembly()` → returns `typeof(PolyEdgeScout.Domain.Entities.Trade).Assembly`
  - `GetApplicationAssembly()` → returns `typeof(PolyEdgeScout.Application.Interfaces.IOrderService).Assembly`
  - `GetInfrastructureAssembly()` → returns `typeof(PolyEdgeScout.Infrastructure.Persistence.TradingDbContext).Assembly`
  - `GetConsoleAssembly()` → returns `typeof(PolyEdgeScout.Console.Program).Assembly` (or entry assembly)
  - `GetPolyEdgeReferencedAssemblies(Assembly assembly)` → filters `GetReferencedAssemblies()` to only `PolyEdgeScout.*` names
  - `IsRecord(Type type)` → checks for compiler-generated `EqualityContract` property

## 2. Layer Dependency Tests

- [x] 2.1 Create `tests/PolyEdgeScout.Architecture.Tests/LayerDependencyTests.cs` with:
  - `Domain_ShouldNotReference_Application` — asserts Domain has no reference to Application assembly
  - `Domain_ShouldNotReference_Infrastructure` — asserts Domain has no reference to Infrastructure assembly
  - `Domain_ShouldNotReference_Console` — asserts Domain has no reference to Console assembly
  - `Application_ShouldReference_Domain` — asserts Application references Domain
  - `Application_ShouldNotReference_Infrastructure` — asserts Application has no reference to Infrastructure
  - `Application_ShouldNotReference_Console` — asserts Application has no reference to Console
  - `Infrastructure_ShouldReference_Domain` — asserts Infrastructure references Domain
  - `Infrastructure_ShouldReference_Application` — asserts Infrastructure references Application
  - `Infrastructure_ShouldNotReference_Console` — asserts Infrastructure has no reference to Console
  - `Console_ShouldReference_Application` — asserts Console references Application
  - `Console_ShouldReference_Infrastructure` — asserts Console references Infrastructure

## 3. Entity Immutability Tests

- [x] 3.1 Create `tests/PolyEdgeScout.Architecture.Tests/EntityImmutabilityTests.cs` with:
  - `AllDomainEntities_ShouldBeSealed` — gets all public types in `PolyEdgeScout.Domain.Entities` namespace, asserts each `IsSealed`
  - `AllDomainEntities_ShouldBeRecords` — gets all public types in `PolyEdgeScout.Domain.Entities` namespace, asserts each is a record (via `EqualityContract` property check)
  - `DomainEntities_ShouldExist` — guard test asserting at least one entity type exists (prevents false positives)

## 4. Interface Placement Tests

- [x] 4.1 Create `tests/PolyEdgeScout.Architecture.Tests/InterfacePlacementTests.cs` with:
  - `RepositoryInterfaces_ShouldBeInApplicationInterfaces` — finds all interfaces ending in "Repository" in the Application assembly, asserts they are in `PolyEdgeScout.Application.Interfaces` namespace
  - `ServiceInterfaces_InApplication_ShouldBeInApplicationInterfaces` — finds all interfaces matching `I*Service` in Application assembly, asserts correct namespace
  - `DomainInterfaces_ShouldBeInDomainInterfaces` — finds all interfaces in Domain assembly, asserts they are in `PolyEdgeScout.Domain.Interfaces`

## 5. Audit Log Constraint Tests

- [x] 5.1 Create `tests/PolyEdgeScout.Architecture.Tests/AuditLogConstraintTests.cs` with:
  - `IAuditLogRepository_ShouldNotHaveUpdateMethods` — reflects on `IAuditLogRepository`, asserts no method name contains "Update"
  - `IAuditLogRepository_ShouldNotHaveDeleteMethods` — asserts no method name contains "Delete"
  - `IAuditLogRepository_ShouldNotHaveRemoveMethods` — asserts no method name contains "Remove"
  - `IAuditLogRepository_ShouldOnlyHaveAddAndQueryMethods` — lists all declared methods and asserts they are known safe methods (Add*, Get*)

## 6. Naming Convention Tests

- [x] 6.1 Create `tests/PolyEdgeScout.Architecture.Tests/NamingConventionTests.cs` with:
  - `RepositoryInterfaces_ShouldFollowNamingConvention` — all interfaces ending in "Repository" should start with "I" and end with "Repository"
  - `ServiceInterfaces_ShouldFollowNamingConvention` — all interfaces ending in "Service" should start with "I" and end with "Service"
  - `DomainEnums_ShouldBeInEnumsNamespace` — all public enum types in `PolyEdgeScout.Domain` should be in `PolyEdgeScout.Domain.Enums` namespace

## 7. Domain Entity Namespace Tests

- [x] 7.1 Create `tests/PolyEdgeScout.Architecture.Tests/DomainEntityNamespaceTests.cs` with:
  - `AllSealedRecords_InDomain_ShouldBeInEntitiesNamespace` — all public sealed record types in the Domain assembly should be in `PolyEdgeScout.Domain.Entities`
  - `KnownEntities_ShouldExistInEntitiesNamespace` — verifies known entity types (`Trade`, `TradeResult`, `AuditLogEntry`, `Market`, `PnlSnapshot`, `AppStateEntry`) exist in the correct namespace

## 8. Domain Isolation Tests

- [x] 8.1 Create `tests/PolyEdgeScout.Architecture.Tests/DomainIsolationTests.cs` with:
  - `Domain_ShouldHaveNoPolyEdgeScoutDependencies` — asserts the Domain assembly has zero references to any other `PolyEdgeScout.*` assembly
  - `Domain_ShouldNotReferenceEntityFramework` — asserts the Domain assembly does not reference any `Microsoft.EntityFrameworkCore` assembly
  - `Domain_ShouldNotReferenceAspNetCore` — asserts Domain does not reference `Microsoft.AspNetCore` assemblies

## 9. Verification

- [x] 9.1 Run `dotnet build` to verify the new test project compiles
- [x] 9.2 Run `dotnet test` on the architecture test project to verify all tests pass
- [x] 9.3 Verify all existing tests still pass (`dotnet test` on entire solution)
