## Why

The PolyEdgeScout application follows .NET Clean Architecture with strict layer boundaries: Domain → Application → Infrastructure → Console. These boundaries are enforced today only by convention and code review. There are no automated tests that verify these constraints, which means:

- A developer could accidentally add a `using PolyEdgeScout.Infrastructure` to the Application layer and it would compile (if the project reference existed) without any test catching it
- Naming conventions (repositories end in "Repository", services end in "Service") are not machine-verified
- The `sealed record` immutability pattern on domain entities could erode over time
- The append-only guarantee of the `IAuditLogRepository` could be violated by adding an `UpdateAsync` or `DeleteAsync` method
- Interface placement rules (all repository/service interfaces in Application/Interfaces) rely on tribal knowledge

Architecture tests act as **living documentation** of the system's structural invariants. They catch violations at build time, not during code review. They also serve as onboarding material — a new developer reads the tests and immediately understands the architecture rules.

## What Changes

Add a dedicated architecture test project (`PolyEdgeScout.Architecture.Tests`) that verifies Clean Architecture layer dependencies, naming conventions, entity immutability, interface placement, and domain-specific constraints (e.g., audit log append-only) using reflection-based xUnit tests. No new runtime dependencies are added — these are compile-time/test-time guards only.

## Capabilities

### New Capabilities
- `architecture-layer-tests`: Automated xUnit tests that verify assembly reference constraints — Domain depends on nothing, Application depends only on Domain, Infrastructure depends on Domain and Application (not Console), Console is the composition root
- `naming-convention-tests`: Tests that verify naming conventions via reflection — repository interfaces end in "Repository", service interfaces end in "Service", implementations follow the same pattern
- `entity-immutability-tests`: Tests that verify all domain entities in the `Entities` namespace are `sealed record` types, enforcing the project's immutability convention
- `interface-placement-tests`: Tests that verify all repository and service interfaces reside in `PolyEdgeScout.Application.Interfaces`, not scattered across layers
- `audit-log-append-only-tests`: Tests that verify `IAuditLogRepository` exposes only `Add*` and query methods — no `Update`, `Delete`, or `Remove` methods
- `domain-isolation-tests`: Tests that verify the Domain assembly has zero project references and no dependencies on external frameworks (only .NET SDK types)

### Modified Capabilities
<!-- None — no existing behavior changes, these are purely additive test-time guards -->

## Impact

- **`tests/PolyEdgeScout.Architecture.Tests/`** — New test project with architecture verification tests
- **`PolyEdgeScout.slnx`** — Updated to include the new test project
- **No changes to src/ projects** — These are test-only additions; no runtime code is modified
- **CI pipeline** — Architecture tests run as part of the normal `dotnet test` execution

### NuGet Packages
- `Microsoft.NET.Test.Sdk` — Test SDK for xUnit runner
- `xunit` — Test framework
- `xunit.runner.visualstudio` — VS Test runner adapter
- `coverlet.collector` — Code coverage (optional, for consistency with other test projects)
