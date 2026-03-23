## Context

PolyEdge Scout is a Terminal.Gui v2 TUI for scanning prediction markets. The current architecture has a single `MainWindow` containing four child `FrameView` panels: `MarketTableView`, `PortfolioView`, `TradesView`, and `LogPanelView`. All views are composed in `MainWindow` and managed by `AppBootstrapper`.

Error visibility has two problems:
1. `FileLogService.Write` calls `Console.WriteLine` which outputs raw text behind the Terminal.Gui surface. On fast-refreshing screens this appears as a momentary cyan/blue "banner" that is immediately overwritten by the TUI redraw — making error messages unreadable.
2. The only in-TUI log display is `LogPanelView` (bottom of dashboard, 50 visible lines). It intermingles all log levels and provides no filtering, making error triage impractical.

The app follows MVVM: ViewModels in `Console/ViewModels/`, Views in `Console/Views/`, with DI registration in `Program.cs`. Key types: `DashboardViewModel` orchestrates scan loop and child VMs; `LogViewModel` maintains a 500-message bounded buffer; `FileLogService` (Infrastructure) implements `ILogService` from Domain.

## Goals / Non-Goals

**Goals:**
- Eliminate the flashing console bleed-through so no raw text appears behind the TUI
- Provide a persistent, dismissible error indicator on the dashboard showing the last error
- Provide a dedicated full-screen Error Log view for reviewing all ERR/WRN messages
- Introduce a lightweight page-switching mechanism so users can toggle between Dashboard and Error Log views
- Forward errors logged anywhere in the infrastructure/application layers to the error log view model

**Non-Goals:**
- Log level filtering UI (e.g., toggleable checkboxes per level) — simple ERR+WRN filter is sufficient for now
- Log search/grep functionality — scrolling is enough for this iteration
- Persistent error log across sessions (file-based logs already cover this)
- Modifying the Domain or Application layers — all changes stay in Infrastructure (logging) and Console
- Supporting more than two views (Dashboard + Error Log) in the navigation system — but the design should not prevent future views

## Decisions

### 1. Remove `Console.WriteLine` from `FileLogService`

**Decision**: Remove the `Console.WriteLine(line)` call from `FileLogService.Write`. Add a `LogEntryWritten` event (`Action<string, string>` — level + formatted line) so the Console layer can subscribe and route to the TUI.

**Rationale**: The raw console write is the root cause of the flashing banner. Terminal.Gui owns the console surface — nothing else should write to it. An event decouples Infrastructure from Console while keeping the `ILogService` interface unchanged.

**Alternative considered**: Making `FileLogService` TUI-aware — rejected because it violates layer boundaries.

### 2. Error indicator as a `Label` inside `MainWindow`

**Decision**: Add an `ErrorIndicatorView` (a simple `FrameView` or `Label` with colored background) positioned at `Y=0` of `MainWindow`, pushing the rest of the dashboard content down by 1 row. It shows the latest error message text and a timestamp. It is hidden when there are no recent errors — auto-clears after a successful scan cycle.

**Rationale**: A simple label is lightweight, doesn't require a new Toplevel, and fits the existing layout model. Using `ColorScheme` with a red/yellow background makes it visually distinct.

**Alternative considered**: Using `MessageBox.ErrorQuery` — rejected because it blocks the UI and requires user dismissal, interrupting background scanning. A toast/notification system was considered but Terminal.Gui v2 has no built-in toast support.

### 3. Full-page Error Log view via `Toplevel` switching

**Decision**: Introduce a `ViewNavigator` class in `Console/App/` that manages switching between the main `DashboardWindow` and a new `ErrorLogWindow` (both inheriting `Window`). The `AppBootstrapper` creates both windows and passes them to `ViewNavigator`. `Ctrl+L` triggers `ViewNavigator.ShowErrorLog()`, `Escape` returns to dashboard. `MenuBar` and `StatusBar` remain on the active window (each window instance gets them).

**Rationale**: Terminal.Gui v2 supports `Application.Run(Toplevel)` for stacking toplevels, but modally. A simpler approach is to swap the content of a single `Toplevel` or use `TabView`. After evaluation, the cleanest approach is to embed both views inside a single `Window` and toggle visibility, since Terminal.Gui v2's `Application.Run` is designed for the outermost loop. Toggling visibility avoids complexity with multiple `Application.Run` calls.

**Implementation**: `MainWindow` will hold both a `DashboardContentView` (the current layout) and an `ErrorLogContentView`. Only one is visible at a time. `ViewNavigator` toggles `Visible` and focus.

**Alternative considered**: Using `TabView` — viable but adds visible tab chrome that takes screen space and changes the aesthetic. Using nested `Application.Run` calls — rejected due to complexity with the main loop. The visibility-toggle approach is the simplest and most predictable.

### 4. `ErrorLogViewModel` with severity filtering

**Decision**: Create `ErrorLogViewModel` in `Console/ViewModels/` with a bounded list of error/warning entries. It subscribes to `FileLogService.LogEntryWritten` (via a bridge wired in DI) and filters for `ERR` and `WRN` levels. Exposes `IReadOnlyList<ErrorLogEntry> Entries` and an `EntryAdded` event.

**Rationale**: Separating error-specific state from `LogViewModel` follows SRP. `LogViewModel` continues to show all messages on the dashboard; `ErrorLogViewModel` provides the filtered, error-focused feed.

### 5. `ILogService` event bridge pattern

**Decision**: Add a `LogEntryWritten` event to `FileLogService` (concrete class, not on the interface). In `Program.cs`, after resolving the `ILogService`, cast to `FileLogService` and wire the event to `ErrorLogViewModel.OnLogEntry`. This avoids modifying the `ILogService` domain interface.

**Alternative considered**: Adding an event to `ILogService` — rejected because events on domain interfaces create coupling. Creating a separate `ILogEventSource` interface — viable for the future but over-engineered for a single subscriber.

## Risks / Trade-offs

- **[Risk] Visibility toggle may cause focus issues** → Mitigation: Explicitly set focus to the first focusable child when toggling views, and call `SetNeedsDraw()` on the parent window.
- **[Risk] Error indicator taking vertical space reduces dashboard area by 1 row** → Mitigation: Only show the indicator when there is an active error; hide it otherwise so dashboard has full height by default.
- **[Risk] Casting `ILogService` to `FileLogService` in `Program.cs` is fragile** → Mitigation: Use a null-check pattern (`if (log is FileLogService fls)`) so it degrades gracefully if the implementation changes. In a future iteration, introduce a proper `ILogEventSource` interface.
- **[Trade-off] Duplicating MenuBar/StatusBar creation for both views** → Acceptable because `MenuBarFactory` is already a static factory; calling it twice is trivial. Shared state lives in ViewModels, not views.
