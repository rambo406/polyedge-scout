## 1. Domain — Edge Formula System

- [x] 1.1 Create `IEdgeFormula` interface in `Domain/Interfaces/` with `string Name { get; }` and `double CalculateEdge(double modelProbability, double marketPrice, double? targetPrice, double? currentAssetPrice)`
- [x] 1.2 Create `DefaultScaledEdgeFormula` in `Domain/Services/` implementing `IEdgeFormula`, extracting current `EdgeCalculation.Edge` logic (base edge × price ratio scaling)
- [x] 1.3 Create `BaseEdgeFormula` in `Domain/Services/` implementing `IEdgeFormula`, computing simple `modelProbability - marketPrice`
- [x] 1.4 Refactor `EdgeCalculation` value object: add `FormulaName` property, change `Edge` from computed to stored, add `Create(IEdgeFormula, ...)` static factory method
- [x] 1.5 Update all existing `EdgeCalculation` usages (`ScanOrchestrationService`, `BacktestService`, tests) to use `EdgeCalculation.Create(formula, ...)` with `DefaultScaledEdgeFormula`
- [x] 1.6 Add unit tests for `IEdgeFormula` implementations: `DefaultScaledEdgeFormula` and `BaseEdgeFormula` (edge cases, typical values, null target/current prices)
- [x] 1.7 Add unit tests for refactored `EdgeCalculation.Create` factory method

## 2. Application — DTOs and Interfaces

- [x] 2.1 Create `EdgeBacktestEntry` record in `Application/DTOs/`: MarketQuestion, ModelProbability, MarketYesPrice, Edge, ActualOutcome, ModelCorrect, Timeframe (string), FormulaName, Symbol, PnL
- [x] 2.2 Create `EdgeFormulaResult` record in `Application/DTOs/`: FormulaName, WinRate, EdgeAccuracy, HypotheticalPnl, Roi, TotalMarkets, MarketsWithEdge, Entries (list of EdgeBacktestEntry)
- [x] 2.3 Create `EdgeSymbolResult` record in `Application/DTOs/`: Symbol, Markets, WinRate, PnL, Roi
- [x] 2.4 Create `EdgeBacktestTimeframeResult` record in `Application/DTOs/`: Timeframe, FormulaResults (list of EdgeFormulaResult), SymbolResults (list of EdgeSymbolResult), TotalMarkets, GrandTotalPnl, GrandTotalRoi
- [x] 2.5 Create `EdgeBacktestResult` record in `Application/DTOs/`: LeftTimeframeResult, RightTimeframeResult, SelectedSymbols (list), FormulaNames (list)
- [x] 2.6 Create `EdgeBacktestProgressEvent` record in `Application/DTOs/`: Entry, FormulaName, Timeframe, EvaluatedCount, TotalCount, RunningMetrics
- [x] 2.7 Create `IEdgeBacktestService` interface in `Application/Interfaces/`: `Task<EdgeBacktestResult> RunEdgeBacktestAsync(List<string> symbols, string leftTimeframe, string rightTimeframe, CancellationToken ct)`, event `EventHandler<EdgeBacktestProgressEvent> OnProgress`

## 3. Application — EdgeBacktestService

- [x] 3.1 Create `EdgeBacktestService` in `Application/Services/` implementing `IEdgeBacktestService`
- [x] 3.2 Implement resolved market fetching and multi-symbol filtering using `IGammaApiClient` and `MarketClassifier.IsCryptoMicro` with keyword matching
- [x] 3.3 Implement edge evaluation loop: for each market × each formula, compute edge via `IEdgeFormula`, determine correctness, build `EdgeBacktestEntry`
- [x] 3.4 Implement concurrent dual-timeframe execution using `Task.WhenAll`
- [x] 3.5 Implement incremental result streaming — raise `OnProgress` after each market with running metrics per formula
- [x] 3.6 Implement per-formula metrics computation: win rate, edge accuracy, P&L ($100 flat bet), ROI
- [x] 3.7 Implement per-symbol breakdown within each formula result
- [x] 3.8 Implement cancellation support — stop evaluating, return partial results without throwing
- [x] 3.9 Implement error handling — skip failed markets, log errors, continue

## 4. Infrastructure Wiring

- [x] 4.1 Register `EdgeBacktestService` as `IEdgeBacktestService` (Singleton) in DI
- [x] 4.2 Register all `IEdgeFormula` implementations in DI (all as Singletons)
- [x] 4.3 Verify `BinanceApiClient.FetchKlinesAsync` supports "1m", "15m", "1h" interval parameters alongside "5m"

## 5. Console — ViewModel

- [x] 5.1 Create `EdgeBacktestViewModel` in `Console/ViewModels/` with properties: Symbols (string), LeftTimeframe, RightTimeframe, IsRunning, LeftTimeframeResult, RightTimeframeResult, AvailableFormulas, AvailableTimeframes
- [x] 5.2 Implement `RunEdgeBacktestAsync` — validates symbols, invokes `IEdgeBacktestService`, forwards progress events
- [x] 5.3 Implement `CancelBacktest` using `CancellationTokenSource`
- [x] 5.4 Add symbol validation against `AppConfig.MarketFilter.IncludeKeywords`
- [x] 5.5 Register `EdgeBacktestViewModel` as Singleton in DI

## 6. Console — View (Tabbed)

- [x] 6.1 Add `ActiveView.EdgeBacktest` to the `ActiveView` enum
- [x] 6.2 Create `EdgeBacktestView` as a `FrameView` implementing `IShortcutHelpProvider` with `Navigator` property and `TabView` containing 3 tabs
- [x] 6.3 Implement config bar: multi-symbol `TextField` (default "BTC"), two timeframe `ComboBox`es (defaults 5m and 15m)
- [x] 6.4 Implement Metrics tab: dual-column layout, left for timeframe A, right for timeframe B
- [x] 6.5 In each Metrics column: formula comparison `TableView` (Formula, Win%, P&L, ROI), per-symbol `TableView`, grand total labels
- [x] 6.6 Implement Charts tab: dual-column with `EquityCurveChart` and `WinRateBarChart` custom views
- [x] 6.7 Create `EquityCurveChart` custom `View` — draws multi-line ASCII chart using Unicode block characters, X=market index, Y=cumulative P&L, one line per formula
- [x] 6.8 Create `WinRateBarChart` custom `View` — horizontal bars using █ blocks, one bar per formula, color-coded (green >55%, yellow 50-55%, red <50%)
- [x] 6.9 Implement Details tab: colored buy/sell signal table with columns Market, Formula, Signal, Outcome (✅/❌), Edge, P&L. Color coded green/red.
- [x] 6.10 Implement `Ctrl+R` shortcut to trigger run, disabled while `IsRunning`
- [x] 6.11 Subscribe to ViewModel progress events, update all tabs via `Application.Invoke()`
- [x] 6.12 Implement `Escape` to return to dashboard, `IShortcutHelpProvider` with Ctrl+R and Escape entries
- [x] 6.13 Implement progress label per timeframe column: "Evaluating X/Y markets..."

## 7. Dashboard Integration

- [x] 7.1 Register `Ctrl+E` in `MainWindow.OnKeyDown` to navigate to `ActiveView.EdgeBacktest`
- [x] 7.2 Wire `EdgeBacktestView` into `AppBootstrapper` and `MainWindow` constructor
- [x] 7.3 Add `EdgeBacktestView` case to `ViewNavigator` show/hide logic
- [x] 7.4 Register all new types in `Program.cs` DI configuration

## 8. Tests

- [x] 8.1 Unit tests for `DefaultScaledEdgeFormula` and `BaseEdgeFormula` (edge cases, typical values, null target/current prices)
- [x] 8.2 Unit tests for refactored `EdgeCalculation.Create` factory
- [x] 8.3 Unit tests for `EdgeBacktestService`: multi-symbol filtering, multi-formula execution, metrics computation, cancellation, error handling
- [x] 8.4 Unit tests for `EdgeBacktestViewModel`: symbol validation, run/cancel lifecycle, progress forwarding
- [x] 8.5 Update `ViewShortcutProviderTests` for `ActiveView.EdgeBacktest`
- [x] 8.6 Verify architecture tests pass with new types
- [x] 8.7 Update existing `EdgeCalculationTests` for the refactored factory method
