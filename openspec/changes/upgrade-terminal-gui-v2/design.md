# Design: Upgrade Terminal.Gui v1.19 → v2

## Target Version

```
Terminal.Gui 2.0.0-develop.5192
```

Pinned to this specific pre-release build for reproducibility. The version wildcard `2.0.0-develop.*` should NOT be used — pre-release versions don't support wildcard matching in NuGet.

## API Migration Map

| v1 API | v2 API | Files Affected |
|--------|--------|----------------|
| `Application.MainLoop?.Invoke(Action)` | `Application.Invoke(Action)` | AppBootstrapper, MarketTableView, PortfolioView, TradesView, LogPanelView, MenuBarFactory |
| `new Window("title")` | `new Window { Title = "title" }` | MainWindow |
| `new FrameView("title")` | `new FrameView { Title = "title" }` | MarketTableView, PortfolioView, TradesView, LogPanelView |
| `new TableView(DataTable)` | `new TableView { Table = ITableSource }` | MarketTableView |
| `DataTable` as table data | `ITableSource` implementation | MarketTableView (new adapter class) |
| `TableView.Update()` | `TableView.SetNeedsDisplay()` | MarketTableView |
| `_table.SelectedRow` | `_table.SelectedRow` | MarketTableView (unchanged) |
| `SelectedCellChanged += (args)` | `SelectedCellChanged += (sender, args)` | MarketTableView |
| `CellActivated += (args)` | `CellActivated += (sender, args)` | MarketTableView |
| `new StatusItem(Key, string, Action)` | `new Shortcut { Key, Title, Action }` | MenuBarFactory |
| `StatusBar(StatusItem[])` | `StatusBar { Shortcuts = [...] }` | MenuBarFactory |
| `statusItem.Title = "~key~ text"` | `shortcut.Title = "text"` (key shown automatically) | MenuBarFactory |
| `statusBar.SetNeedsDisplay()` | `statusBar.SetNeedsDisplay()` | MenuBarFactory (unchanged) |
| `Key.Q \| Key.CtrlMask` | `Key.Q.WithCtrl` | MenuBarFactory |
| `Key.T \| Key.CtrlMask` | `Key.T.WithCtrl` | MenuBarFactory |
| `Key.F5` | `Key.F5` | MenuBarFactory (unchanged) |
| `Key.Null` | `Key.Empty` | MenuBarFactory |
| `new MenuItem(title, help, action, shortcut:)` | `new MenuItem { Title, Help, Action, ShortcutKey }` | MenuBarFactory |
| `new MenuBarItem(title, menuItems)` | `new MenuBarItem { Title, Children }` | MenuBarFactory |
| `ListView(source)` constructor | `new ListView { Source = new ListWrapper(source) }` | TradesView, LogPanelView |
| `_listView.SetSource(list)` | `_listView.Source = new ListWrapper<string>(list)` | TradesView, LogPanelView |
| `_listView.SelectedItem = n` | `_listView.SelectedItem = n` | LogPanelView (unchanged) |
| `Label("text")` | `new Label { Text = "text" }` | PortfolioView |
| `label.Text = "..."` | `label.Text = "..."` | PortfolioView (unchanged) |
| `Dim.Percent(n)` | `Dim.Percent(n)` | MainWindow (unchanged) |
| `Dim.Fill()` | `Dim.Fill()` | All Views (unchanged) |
| `Pos.Right(view)` | `Pos.Right(view)` | MainWindow (unchanged) |
| `Pos.Bottom(view)` | `Pos.Bottom(view)` | MainWindow (unchanged) |
| `X = 0`, `Y = 0` | `X = 0`, `Y = 0` | All Views (unchanged — `0` int works as `Pos.Absolute(0)`) |
| `Application.Top.Add(...)` | `Application.Top.Add(...)` | AppBootstrapper (unchanged) |
| `Application.Init()` | `Application.Init()` | AppBootstrapper (unchanged) |
| `Application.Run()` | `Application.Run()` | AppBootstrapper (unchanged) |
| `Application.Shutdown()` | `Application.Shutdown()` | AppBootstrapper (unchanged) |
| `Application.RequestStop()` | `Application.RequestStop()` | AppBootstrapper (unchanged) |

## Design Decisions

### 1. Pin to Specific Pre-Release Version

```xml
<PackageReference Include="Terminal.Gui" Version="2.0.0-develop.5192" />
```

Pre-release versions cannot use wildcards. Pinning ensures deterministic builds and prevents surprise breakage from new dev builds.

### 2. Create `MarketTableSource : ITableSource` Adapter

v2 replaces `System.Data.DataTable` with the `ITableSource` interface for `TableView`. Instead of converting ViewModel data → DataTable rows → display, create a direct adapter:

```csharp
public sealed class MarketTableSource : ITableSource
{
    private readonly IReadOnlyList<MarketScanResult> _markets;
    private readonly string[] _columnNames = ["#", "Market Question", "YES", "Volume", "Model", "Edge", "Action"];

    public MarketTableSource(IReadOnlyList<MarketScanResult> markets)
    {
        _markets = markets;
    }

    public object this[int row, int col] => col switch
    {
        0 => (row + 1).ToString(),
        1 => TruncateQuestion(_markets[row].Market.Question),
        2 => _markets[row].Market.YesPrice.ToString("F2"),
        3 => $"${_markets[row].Market.Volume:N0}",
        4 => _markets[row].ModelProbability.ToString("F2"),
        5 => _markets[row].Edge.ToString("+0.00;-0.00"),
        6 => _markets[row].Action,
        _ => ""
    };

    public int Rows => Math.Min(_markets.Count, 20);
    public int Columns => _columnNames.Length;
    public string[] ColumnNames => _columnNames;

    private static string TruncateQuestion(string q) =>
        q.Length > 45 ? q[..44] + "…" : q;
}
```

This eliminates the `System.Data` dependency from MarketTableView and the DataTable row-copy loop.

### 3. Use v2 Key Syntax

v1 uses bitmask composition: `Key.Q | Key.CtrlMask`
v2 uses fluent extensions: `Key.Q.WithCtrl`

All 5 key binding sites in MenuBarFactory will be migrated.

### 4. StatusBar Migration to Shortcut

v1's `StatusItem(Key, title, action)` becomes v2's `Shortcut` with properties:

```csharp
new Shortcut
{
    Key = Key.Q.WithCtrl,
    Title = "Quit",
    Action = () => vm.RequestQuit()
}
```

The v1 tilde-markup pattern (`~Ctrl-Q~ Quit`) is no longer needed — v2 renders the key automatically from the `Key` property.

### 5. Keep MVVM Pattern Intact

All changes are strictly in the View layer:

```
PolyEdgeScout.Console/
├── App/
│   └── AppBootstrapper.cs      ← CHANGED (MainLoop.Invoke → Invoke)
├── Views/
│   ├── MainWindow.cs           ← CHANGED (constructor)
│   ├── MarketTableView.cs      ← CHANGED (ITableSource, events, Invoke)
│   ├── MarketTableSource.cs    ← NEW (ITableSource adapter)
│   ├── PortfolioView.cs        ← CHANGED (constructor, Invoke)
│   ├── TradesView.cs           ← CHANGED (constructor, ListView, Invoke)
│   ├── LogPanelView.cs         ← CHANGED (constructor, ListView, Invoke)
│   └── MenuBarFactory.cs       ← CHANGED (StatusBar, keys, MenuBar)
├── ViewModels/                  ← UNCHANGED
│   ├── DashboardViewModel.cs
│   ├── MarketTableViewModel.cs
│   ├── PortfolioViewModel.cs
│   ├── TradesViewModel.cs
│   ├── LogViewModel.cs
│   └── BacktestViewModel.cs
```

### 6. No Changes to Tests

All existing tests target ViewModels, which have zero Terminal.Gui dependency. They must pass unchanged after the migration — this is the primary verification that the upgrade is safe.

### 7. Layout System — Mostly Compatible

The v2 `Pos` and `Dim` classes maintain the same static factory methods used in our code:
- `Dim.Fill()` — unchanged
- `Dim.Percent(n)` — unchanged
- `Pos.Right(view)`, `Pos.Bottom(view)` — unchanged
- `X = 0`, `Y = 0` — integer assignment still works (implicit `Pos.Absolute`)

No layout changes are required. `Dim.Auto()` is available but not needed for this migration (can be adopted in a follow-up).

### 8. Event Handler Signatures

v2 changes event handler signatures from `Action<EventArgs>` to `EventHandler<EventArgs>`:

```csharp
// v1
_table.SelectedCellChanged += (args) => { ... };
_table.CellActivated += (args) => { ... };

// v2
_table.SelectedCellChanged += (sender, args) => { ... };
_table.CellActivated += (sender, args) => { ... };
```

Both sites are in MarketTableView.
