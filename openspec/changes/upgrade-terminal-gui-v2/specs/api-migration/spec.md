# Spec: Terminal.Gui v1 → v2 API Migration

## REQ-1: Package Reference Update

### REQ-1.1: Version Update

The `PolyEdgeScout.Console.csproj` package reference MUST be updated from:

```xml
<PackageReference Include="Terminal.Gui" Version="1.19.*" />
```

to:

```xml
<PackageReference Include="Terminal.Gui" Version="2.0.0-develop.5192" />
```

### REQ-1.2: DataTable Dependency Removal

After migration, `MarketTableView.cs` MUST NOT import `System.Data`. The `DataTable` intermediary is replaced by `ITableSource`.

### REQ-1.3: Restore Verification

`dotnet restore` MUST succeed with the new pre-release package version.

---

## REQ-2: Application.Invoke Migration

### REQ-2.1: MainLoop.Invoke Elimination

All occurrences of `Application.MainLoop?.Invoke(Action)` MUST be replaced with `Application.Invoke(Action)`.

**Affected call sites (8 total):**

| File | Line (approx) | Context |
|------|--------------|---------|
| `AppBootstrapper.cs` | 53 | `Application.MainLoop?.Invoke(() => Application.RequestStop())` |
| `MarketTableView.cs` | 60 | Market data refresh |
| `PortfolioView.cs` | 35 | Portfolio snapshot update |
| `TradesView.cs` | 34 | Trades list refresh |
| `LogPanelView.cs` | 34 | Log message append |
| `MenuBarFactory.cs` | 42 | Mode change status update |
| `MenuBarFactory.cs` | 54 | Scan status update |
| `MenuBarFactory.cs` | 69 | Bankroll status update |

### REQ-2.2: Null-Conditional Removal

The `?.` null-conditional on `MainLoop` is no longer needed. `Application.Invoke()` is a static method that is always available after `Application.Init()`.

**Scenario: Thread marshalling works after migration**
- Given the application is initialized with `Application.Init()`
- When a background thread calls `Application.Invoke(action)`
- Then the action executes on the UI thread

---

## REQ-3: Window and FrameView Constructor Migration

### REQ-3.1: Window Constructor

`MainWindow` MUST change from parameterised constructor to object initializer:

```csharp
// v1
public MainWindow(...) : base("PolyEdge Scout v1.0")

// v2
public MainWindow(...) : base()
// with Title = "PolyEdge Scout v1.0" set in constructor body or initializer
```

### REQ-3.2: FrameView Constructor

All `FrameView` subclasses MUST change from parameterised constructor to object initializer pattern:

| Class | v1 Title | v2 Title Property |
|-------|----------|------------------|
| `MarketTableView` | `base("Market Scanner")` | `Title = "Market Scanner"` |
| `PortfolioView` | `base("Portfolio")` | `Title = "Portfolio"` |
| `TradesView` | `base("Last 5 Trades")` | `Title = "Last 5 Trades"` |
| `LogPanelView` | `base("Log")` | `Title = "Log"` |

**Scenario: FrameView displays title after migration**
- Given a FrameView subclass with `Title = "Market Scanner"` set in constructor
- When the view is rendered
- Then the frame border displays "Market Scanner" as the title

---

## REQ-4: TableView Migration to ITableSource

### REQ-4.1: MarketTableSource Implementation

A new class `MarketTableSource` MUST be created implementing `Terminal.Gui.ITableSource`:

- `this[int row, int col]` — returns cell value for the given row/column
- `Rows` — returns `Math.Min(markets.Count, 20)`
- `Columns` — returns 7 (fixed column count)
- `ColumnNames` — returns `["#", "Market Question", "YES", "Volume", "Model", "Edge", "Action"]`

### REQ-4.2: DataTable Removal

`MarketTableView` MUST remove:
- The `DataTable _dataTable` field
- The `System.Data` using directive
- The `_dataTable.Columns.Add(...)` calls
- The `_dataTable.Rows.Clear()` / `_dataTable.Rows.Add(...)` loop

### REQ-4.3: TableView Binding

```csharp
// v1
_table = new TableView(_dataTable) { ... };
_table.Update();

// v2
_table = new TableView { Table = new MarketTableSource(emptyList), ... };
_table.Table = new MarketTableSource(markets); // on update
_table.SetNeedsDisplay();
```

### REQ-4.4: TableView Event Signatures

Event handlers MUST be updated to include `sender` parameter:

```csharp
// v1
_table.SelectedCellChanged += (args) => { ... };
_table.CellActivated += (args) => { ... };

// v2
_table.SelectedCellChanged += (sender, args) => { ... };
_table.CellActivated += (sender, args) => { ... };
```

**Scenario: Market data displays in table after migration**
- Given MarketTableViewModel emits `MarketsUpdated` with 10 markets
- When the event handler creates a new `MarketTableSource`
- Then the TableView displays 10 rows with correct column values

---

## REQ-5: StatusBar Migration

### REQ-5.1: StatusItem → Shortcut

All `StatusItem` instances MUST be replaced with `Shortcut`:

| v1 StatusItem | v2 Shortcut |
|--------------|-------------|
| `new StatusItem(Key.T \| Key.CtrlMask, "~Ctrl-T~ Mode: PAPER", action)` | `new Shortcut { Key = Key.T.WithCtrl, Title = "Mode: PAPER", Action = action }` |
| `new StatusItem(Key.Null, "Bankroll: $10,000.00", null)` | `new Shortcut { Key = Key.Empty, Title = "Bankroll: $10,000.00" }` |
| `new StatusItem(Key.F5, "~F5~ Refresh", action)` | `new Shortcut { Key = Key.F5, Title = "Refresh", Action = action }` |
| `new StatusItem(Key.Q \| Key.CtrlMask, "~Ctrl-Q~ Quit", action)` | `new Shortcut { Key = Key.Q.WithCtrl, Title = "Quit", Action = action }` |

### REQ-5.2: Tilde Markup Removal

v1's `~key~` markup prefix in status item titles MUST be removed. v2 renders the key from the `Key` property automatically.

### REQ-5.3: StatusBar Constructor

```csharp
// v1
new StatusBar(new StatusItem[] { ... })

// v2
new StatusBar { Shortcuts = { shortcut1, shortcut2, ... } }
```

### REQ-5.4: Dynamic Status Updates

Status bar items that are updated dynamically (mode, bankroll, scan status) MUST update the `Shortcut.Title` property instead of `StatusItem.Title`, still calling `statusBar.SetNeedsDisplay()` afterwards.

**Scenario: Mode toggle updates status bar**
- Given the status bar shows "Mode: PAPER"
- When the user triggers Ctrl+T to toggle mode
- Then the shortcut title updates to "Mode: LIVE"

---

## REQ-6: MenuBar and MenuItem Migration

### REQ-6.1: MenuItem Constructor

```csharp
// v1
new MenuItem("_Quit", "Ctrl+Q", () => vm.RequestQuit(), shortcut: Key.Q | Key.CtrlMask)

// v2
new MenuItem
{
    Title = "_Quit",
    Help = "Ctrl+Q",
    Action = () => vm.RequestQuit(),
    ShortcutKey = Key.Q.WithCtrl
}
```

### REQ-6.2: MenuBarItem Constructor

```csharp
// v1
new MenuBarItem("_File", new MenuItem[] { ... })

// v2
new MenuBarItem
{
    Title = "_File",
    Children = new MenuItem[] { ... }
}
```

### REQ-6.3: MenuBar Construction

The `MenuBar` constructor still accepts `MenuBarItem[]`:

```csharp
new MenuBar(new MenuBarItem[] { ... })
```

This pattern is mostly unchanged.

---

## REQ-7: Key Binding Migration

### REQ-7.1: CtrlMask → WithCtrl

All `Key.X | Key.CtrlMask` patterns MUST be replaced with `Key.X.WithCtrl`:

| v1 | v2 |
|----|-----|
| `Key.Q \| Key.CtrlMask` | `Key.Q.WithCtrl` |
| `Key.T \| Key.CtrlMask` | `Key.T.WithCtrl` |

### REQ-7.2: Key.Null → Key.Empty

`Key.Null` MUST be replaced with `Key.Empty`.

### REQ-7.3: Function Keys

`Key.F5` remains unchanged in v2.

---

## REQ-8: ListView Source Binding Migration

### REQ-8.1: ListView Constructor

```csharp
// v1
new ListView(list) { ... }

// v2
new ListView { Source = new ListWrapper<string>(list), ... }
```

Affected files: `TradesView.cs`, `LogPanelView.cs`

### REQ-8.2: SetSource Replacement

```csharp
// v1
_listView.SetSource(_items);

// v2
_listView.Source = new ListWrapper<string>(_items);
```

Affected files: `TradesView.cs`, `LogPanelView.cs`

**Scenario: Log messages display after migration**
- Given LogViewModel emits `MessageAdded` with 50 messages
- When LogPanelView handles the event
- Then the ListView shows the last 50 messages with the last item selected (auto-scroll)

---

## REQ-9: Layout System Compatibility

### REQ-9.1: Pos and Dim Compatibility

The following v1 Pos/Dim APIs are unchanged in v2 and require NO migration:

- `Dim.Fill()` — fills remaining space
- `Dim.Percent(n)` — percentage of parent
- `Pos.Right(view)` — position to right of sibling
- `Pos.Bottom(view)` — position below sibling
- `X = 0`, `Y = 0` — integer assignment (implicit `Pos.Absolute`)

### REQ-9.2: No Dim.Auto() Adoption

This migration does NOT adopt `Dim.Auto()` for automatic sizing. Layout remains equivalent to v1. `Dim.Auto()` may be adopted in a future enhancement.

---

## REQ-10: Build and Test Verification

### REQ-10.1: Compilation

`dotnet build` MUST succeed with zero errors and zero warnings related to Terminal.Gui API usage.

### REQ-10.2: Test Suite

`dotnet test` MUST pass all existing tests. Since ViewModels have zero Terminal.Gui dependency, all ViewModel tests MUST pass unchanged.

### REQ-10.3: No v1 API Remnants

After migration, a codebase search for the following patterns MUST return zero results:
- `Application.MainLoop`
- `Key.CtrlMask`
- `Key.AltMask`
- `Key.Null`
- `StatusItem`
- `new Window("`
- `new FrameView("`
- `System.Data` in View files
- `SetSource(`
