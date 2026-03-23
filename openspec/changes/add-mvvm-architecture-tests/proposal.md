## Why

The PolyEdgeScout Console layer follows an MVVM (Model-View-ViewModel) architecture using Terminal.Gui v2. The existing architecture test suite (30 tests in `PolyEdgeScout.Architecture.Tests`) covers Domain, Application, and Infrastructure layer constraints but has **zero coverage** of the Console layer's MVVM patterns. This means:

- A developer could add a `using Terminal.Gui` to a ViewModel, coupling presentation framework details into what should be pure C# logic — no test catches this
- Views could bypass ViewModels and call Application/Domain services directly, breaking the MVVM binding pattern
- ViewModels could reference View types, creating circular dependencies
- Naming conventions (`*ViewModel`, `*View`, `*Window`) are enforced only by code review
- The `sealed` convention on ViewModels could erode silently
- Terminal.Gui subclasses could scatter outside the `Views` namespace

Today, all ViewModels are clean — they have no Terminal.Gui references, no View references, are properly sealed, and follow naming conventions. But without automated tests, this discipline is fragile. These tests act as **regression guardrails**: they encode the current healthy state and prevent future violations at build time.

## What Changes

Add MVVM convention tests to the existing `PolyEdgeScout.Architecture.Tests` project. This requires adding a project reference from `Architecture.Tests → Console`, adding a `GetConsoleAssembly()` helper to the existing `AssemblyHelper` class, and creating a single new test file (`MvvmConventionTests.cs`) with ~10 reflection-based xUnit tests. No runtime code changes. All tests should pass immediately since they codify the current architecture.

## Capabilities

### New Capabilities

- `viewmodel-no-terminal-gui`: Verifies that no ViewModel type in `PolyEdgeScout.Console.ViewModels` references any Terminal.Gui assembly — core MVVM separation rule ensuring ViewModels remain pure C# logic
- `viewmodel-no-view-refs`: Verifies that ViewModel types do not reference View types from `PolyEdgeScout.Console.Views` — prevents circular dependencies between Views and ViewModels
- `viewmodel-naming-convention`: Verifies all public classes in the `ViewModels` namespace end with "ViewModel" — self-documenting, discoverable naming
- `view-naming-convention`: Verifies all public classes in the `Views` namespace end with "View", "Window", "Source", or "Factory" — self-documenting naming with allowances for supporting types
- `view-injects-viewmodel`: Verifies that View classes accept a ViewModel via constructor injection — enforces the standard MVVM binding pattern (with documented exemptions for `MainWindow` and `MarketTableSource`)
- `viewmodel-sealed`: Verifies all ViewModel classes are sealed — prevents inheritance abuse, keeps the ViewModel hierarchy flat
- `viewmodel-namespace`: Verifies all ViewModel types live in the `PolyEdgeScout.Console.ViewModels` namespace — prevents scattering across the project
- `terminal-gui-subclass-namespace`: Verifies all types inheriting from Terminal.Gui base classes reside in the `Views` namespace — keeps UI framework coupling contained
- `view-no-service-refs`: Verifies that View types do not directly reference Application or Domain service interfaces — Views should talk only to ViewModels
- `view-no-cross-viewmodel-refs`: Verifies that View types only reference their own ViewModel, not other Views' ViewModels — prevents spaghetti cross-referencing

### Modified Capabilities

- `architecture-test-helpers`: The existing `AssemblyHelper` class gains a `GetConsoleAssembly()` method to load the Console assembly for reflection-based testing

## Impact

- **`tests/PolyEdgeScout.Architecture.Tests/MvvmConventionTests.cs`** — New test file containing ~10 MVVM architecture tests
- **`tests/PolyEdgeScout.Architecture.Tests/Helpers/AssemblyHelper.cs`** — Add `GetConsoleAssembly()` method
- **`tests/PolyEdgeScout.Architecture.Tests/PolyEdgeScout.Architecture.Tests.csproj`** — Add project reference to `PolyEdgeScout.Console`
- **No changes to src/ projects** — These are test-only additions; no runtime code is modified
- **CI pipeline** — MVVM convention tests run as part of the normal `dotnet test` execution

### NuGet Packages

No new NuGet packages required — the existing `Architecture.Tests` project already has xUnit, Microsoft.NET.Test.Sdk, and xunit.runner.visualstudio.

## Non-Goals

- **1:1 View↔ViewModel pairing** — Would fail today since `BacktestViewModel` and `DashboardViewModel` have legitimate unpaired types. Not enforced.
- **Migrating to NetArchTest or ArchUnitNET** — Keep the current pure `System.Reflection` approach consistent with the existing 30 architecture tests.
- **Testing View layout or rendering** — These are structural/convention tests only, not UI integration tests.
- **Modifying any runtime code** — All changes are in the test project; the Console project is untouched.
