# Proposal: Upgrade Terminal.Gui from v1.19 to v2

## Summary

Upgrade the Terminal.Gui NuGet package from `1.19.*` (stable) to `2.0.0-develop.5192` (pre-release) in the PolyEdgeScout.Console project to adopt the modern v2 API.

## Motivation

### Why Upgrade?

1. **Modern API design** — v2 introduces a cleaner, more consistent API surface. The layout system gains `Dim.Auto()`, the event model shifts to `EventArgs`-based patterns, and the `Command`/`KeyBindings` system replaces ad-hoc key handling.

2. **Simplified thread marshalling** — `Application.Invoke()` replaces the verbose `Application.MainLoop?.Invoke()` pattern used in all 6 View files and AppBootstrapper (8 call sites total). This eliminates null-conditional checks and reduces ceremony.

3. **Better TableView with ITableSource** — The current `MarketTableView` creates a `System.Data.DataTable` as an intermediary. v2's `ITableSource` interface allows a direct adapter over ViewModel data, removing the DataTable allocation and row-copy loop.

4. **Improved keyboard/command system** — v2's `Command` enum and `KeyBindings` system provides declarative, composable key binding instead of `Key.Q | Key.CtrlMask` bitmask patterns.

5. **New theming system** — v2 replaces `ColorScheme` with a proper theming API, enabling future UI customization.

6. **Alignment with eventual stable release** — Terminal.Gui v2 has been in active development for years. Migrating now means we track the API as it stabilises rather than facing a larger migration delta later.

### Why Now?

The MVVM architecture is mature. Views are thin wrappers around ViewModels — the entire migration is isolated to 7 files in the Console project. All ViewModel tests remain unaffected.

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Pre-release API may have bugs | Medium | Medium | Pin to specific version `2.0.0-develop.5192`; can roll back |
| v2 API may change before stable | Medium | Low | Changes between dev builds are incremental at this stage |
| Rendering differences | Low | Low | Manual visual testing of dashboard layout |
| Missing v2 documentation | Medium | Low | v2 repo has extensive examples and migration guide |

## Impact Analysis

### Files Requiring Changes (7 files)

| File | Changes Required |
|------|-----------------|
| `PolyEdgeScout.Console.csproj` | Package version update |
| `App/AppBootstrapper.cs` | `MainLoop.Invoke` → `Application.Invoke`, `Application.Top` usage |
| `Views/MainWindow.cs` | `Window` constructor change, layout Pos/Dim review |
| `Views/MarketTableView.cs` | `DataTable` → `ITableSource`, `MainLoop.Invoke` → `Invoke`, event args |
| `Views/PortfolioView.cs` | `FrameView` constructor, `MainLoop.Invoke` → `Invoke` |
| `Views/TradesView.cs` | `FrameView` constructor, `ListView.SetSource` → `Source`, `MainLoop.Invoke` → `Invoke` |
| `Views/LogPanelView.cs` | `FrameView` constructor, `ListView.SetSource` → `Source`, `MainLoop.Invoke` → `Invoke` |
| `Views/MenuBarFactory.cs` | `StatusItem` → `Shortcut`, `Key.CtrlMask` → v2 key syntax, `MenuBarItem`/`MenuItem` restructure |

### Files NOT Affected

- **All ViewModels** — Confirmed zero Terminal.Gui references (MVVM isolation verified)
- **Domain layer** — No UI dependency
- **Application layer** — No UI dependency
- **Infrastructure layer** — No UI dependency
- **All existing tests** — ViewModel tests don't touch Terminal.Gui

## Non-Goals

- Adopting v2's `ConfigurationManager` or theme files — keep default theme
- Switching from `Window` to `Toplevel` as root container (not required)
- Adding new View features — this is a like-for-like API migration
- Creating View-level integration tests (future change)
