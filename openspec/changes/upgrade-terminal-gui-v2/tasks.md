# Tasks: Upgrade Terminal.Gui v1.19 → v2

## Group 1: Package Update

- [x] Update `src/PolyEdgeScout.Console/PolyEdgeScout.Console.csproj` — change `Terminal.Gui` version from `1.19.*` to `2.0.0-develop.5192`
- [x] Run `dotnet restore` and verify the pre-release package resolves successfully
- [x] Verify `dotnet build` output to identify all v1 API compilation errors (expected — used as migration checklist)

## Group 2: AppBootstrapper Migration

- [x] In `src/PolyEdgeScout.Console/App/AppBootstrapper.cs`:
  - Replace `Application.MainLoop?.Invoke(() => Application.RequestStop())` → `Application.Invoke(() => Application.RequestStop())`
  - Verify `Application.Init()`, `Application.Run()`, `Application.Shutdown()` are unchanged (they are compatible)
  - Verify `Application.Top.Add(...)` is unchanged (compatible)

## Group 3: MainWindow Migration

- [x] In `src/PolyEdgeScout.Console/Views/MainWindow.cs`:
  - Change `Window` constructor from `: base("PolyEdge Scout v1.0")` to `: base()` and set `Title = "PolyEdge Scout v1.0"` in constructor body
  - Verify all `Pos.Right()`, `Pos.Bottom()`, `Dim.Percent()`, `Dim.Fill()` calls compile (they should — API compatible)

## Group 4: MarketTableView Migration

- [x] Create new file `src/PolyEdgeScout.Console/Views/MarketTableSource.cs`:
  - Implement `ITableSource` interface with 7 columns matching current DataTable columns
  - Accept `IReadOnlyList<MarketScanResult>` in constructor
  - Cap rows at 20 (matching current behaviour)
  - Truncate question to 44 chars + "…" (matching current behaviour)
- [x] In `src/PolyEdgeScout.Console/Views/MarketTableView.cs`:
  - Remove `using System.Data` import
  - Remove `DataTable _dataTable` field and all `_dataTable.Columns.Add(...)` setup
  - Change `FrameView` constructor from `: base("Market Scanner")` to `: base()` with `Title = "Market Scanner"` in body
  - Change `new TableView(_dataTable)` to `new TableView { Table = new MarketTableSource(Array.Empty<MarketScanResult>()), ... }`
  - Update `SelectedCellChanged += (args)` to `SelectedCellChanged += (sender, args)`
  - Update `CellActivated += (args)` to `CellActivated += (sender, args)`
  - In `OnMarketsUpdated()`: replace DataTable row-copy loop with `_table.Table = new MarketTableSource(_vm.Markets)`
  - Replace `_table.Update()` with `_table.SetNeedsDisplay()`
  - Replace `Application.MainLoop?.Invoke(...)` with `Application.Invoke(...)`

## Group 5: PortfolioView Migration

- [x] In `src/PolyEdgeScout.Console/Views/PortfolioView.cs`:
  - Change `FrameView` constructor from `: base("Portfolio")` to `: base()` with `Title = "Portfolio"` in body
  - Change `Label` constructors from `new Label("text") { X = ..., Y = ... }` to `new Label { Text = "text", X = ..., Y = ... }`
  - Replace `Application.MainLoop?.Invoke(...)` with `Application.Invoke(...)`

## Group 6: TradesView Migration

- [x] In `src/PolyEdgeScout.Console/Views/TradesView.cs`:
  - Change `FrameView` constructor from `: base("Last 5 Trades")` to `: base()` with `Title = "Last 5 Trades"` in body
  - Change `new ListView(_items) { ... }` to `new ListView { Source = new ListWrapper<string>(_items), ... }`
  - Replace `_listView.SetSource(_items)` with `_listView.Source = new ListWrapper<string>(_items)`
  - Replace `Application.MainLoop?.Invoke(...)` with `Application.Invoke(...)`

## Group 7: LogPanelView Migration

- [x] In `src/PolyEdgeScout.Console/Views/LogPanelView.cs`:
  - Change `FrameView` constructor from `: base("Log")` to `: base()` with `Title = "Log"` in body
  - Change `new ListView(_items) { ... }` to `new ListView { Source = new ListWrapper<string>(_items), ... }`
  - Replace `_listView.SetSource(_items)` with `_listView.Source = new ListWrapper<string>(_items)`
  - Replace `Application.MainLoop?.Invoke(...)` with `Application.Invoke(...)`
  - Verify `_listView.SelectedItem = _items.Count - 1` auto-scroll still works

## Group 8: MenuBarFactory Migration

- [x] In `src/PolyEdgeScout.Console/Views/MenuBarFactory.cs`:
  - **MenuBar/MenuItem migration:**
    - Update `new MenuBarItem("_File", new MenuItem[] { ... })` to use v2 constructor/initializer syntax
    - Update `new MenuItem("_Quit", "Ctrl+Q", action, shortcut: key)` to v2 initializer syntax
    - Repeat for all 3 menu bar items (File, View, Trading) and their children
  - **Key binding migration:**
    - Replace `Key.Q | Key.CtrlMask` → `Key.Q.WithCtrl`
    - Replace `Key.T | Key.CtrlMask` → `Key.T.WithCtrl`
    - Replace `Key.Null` → `Key.Empty`
    - Keep `Key.F5` as-is
  - **StatusBar migration:**
    - Replace all `new StatusItem(...)` with `new Shortcut { Key = ..., Title = ..., Action = ... }`
    - Replace `new StatusBar(new StatusItem[] { ... })` with `new StatusBar { Shortcuts = { ... } }`
    - Remove tilde markup from titles (`~Ctrl-T~`, `~F5~`, `~Ctrl-Q~`)
    - Update dynamic title assignments to use `Shortcut.Title`
  - Replace all 3 `Application.MainLoop?.Invoke(...)` with `Application.Invoke(...)`

## Group 9: Build & Test Verification

- [x] Run `dotnet build` — must succeed with zero Terminal.Gui-related errors
- [x] Run `dotnet test` — all existing ViewModel tests must pass unchanged
- [x] Search codebase for v1 API remnants — must find zero matches for:
  - `Application.MainLoop`
  - `Key.CtrlMask` / `Key.AltMask` / `Key.Null`
  - `StatusItem`
  - `new Window("` / `new FrameView("`
  - `System.Data` in View files
  - `SetSource(`
- [ ] Manual smoke test: run the app and verify dashboard renders correctly with all panels
