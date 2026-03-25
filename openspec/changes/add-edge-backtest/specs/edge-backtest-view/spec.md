## ADDED Requirements

### Requirement: R1 — Three-tab TabView layout
The system SHALL provide an `EdgeBacktestView` with a `TabView` containing three tabs: Metrics, Charts, and Details.

#### Scenario: Viewing Metrics tab
- **WHEN** the user opens the EdgeBacktestView
- **THEN** the default Metrics tab SHALL be visible with formula comparison tables and per-symbol breakdowns

#### Scenario: Switching tabs
- **WHEN** the user switches between Metrics, Charts, and Details tabs
- **THEN** each tab SHALL display its respective content

### Requirement: R2 — Config bar
The system SHALL provide a config bar with a multi-symbol `TextField` (default "BTC"), two timeframe `ComboBox` selectors (defaults 5m and 15m), and a `Ctrl+R` trigger to start the backtest.

#### Scenario: Default configuration
- **WHEN** the EdgeBacktestView is opened for the first time
- **THEN** the symbol field SHALL default to "BTC", left timeframe to "5m", right timeframe to "15m"

#### Scenario: Multi-symbol entry
- **WHEN** the user types "BTC, ETH, SOL" in the symbol field
- **THEN** the system SHALL parse and validate all three symbols

#### Scenario: Timeframe selection
- **WHEN** the user opens a timeframe ComboBox
- **THEN** the options SHALL be [1m, 5m, 15m, 1h]

### Requirement: R3 — Side-by-side timeframe columns
Every tab SHALL maintain a side-by-side dual-column layout: left column for timeframe A, right column for timeframe B.

#### Scenario: Dual-column display
- **WHEN** the backtest completes for both timeframes
- **THEN** each tab SHALL display left column results for the first selected timeframe and right column results for the second

#### Scenario: Independent column updates
- **WHEN** one timeframe finishes before the other
- **THEN** the completed column SHALL display its results immediately

### Requirement: R4 — Metrics tab content
The Metrics tab SHALL display: formula comparison `TableView` (columns: Formula, Win%, P&L, ROI), per-symbol breakdown `TableView`, and grand total labels — in each timeframe column.

#### Scenario: Formula comparison table
- **WHEN** results include DefaultScaledEdge and BaseEdge formulas
- **THEN** the table SHALL show one row per formula with Win%, P&L, and ROI

#### Scenario: Per-symbol breakdown
- **WHEN** results include BTC and ETH symbols
- **THEN** the breakdown SHALL show separate rows for BTC and ETH with their metrics

#### Scenario: Grand total
- **WHEN** all formula results are computed
- **THEN** grand total labels SHALL show combined P&L and ROI across all formulas

### Requirement: R5 — Charts tab content
The Charts tab SHALL display ASCII equity curves (multi-line per formula) and win rate horizontal bar charts in each timeframe column.

#### Scenario: Equity curve rendering
- **WHEN** the Charts tab is viewed with completed results
- **THEN** an `EquityCurveChart` SHALL render with X=market index, Y=cumulative P&L, one line per formula using Unicode block characters

#### Scenario: Win rate bar chart rendering
- **WHEN** the Charts tab is viewed with completed results
- **THEN** a `WinRateBarChart` SHALL render horizontal bars using █ blocks, one bar per formula, color-coded (green >55%, yellow 50-55%, red <50%)

### Requirement: R6 — Details tab content
The Details tab SHALL display a colored buy/sell signal table with columns: Market, Formula, Signal, Outcome (✅/❌), Edge, P&L. Rows SHALL be color-coded green (win) / red (loss), scrollable.

#### Scenario: Signal table display
- **WHEN** the Details tab is viewed with completed results
- **THEN** the table SHALL list every evaluated market entry with formula, signal direction, outcome icon, edge value, and P&L

#### Scenario: Color coding
- **WHEN** a row represents a winning trade
- **THEN** the row SHALL be displayed in green; losing trades in red

### Requirement: R7 — Live streaming updates
The view SHALL update tables and charts in real-time as results stream in, using `Application.Invoke()` for thread-safe UI dispatch.

#### Scenario: Streamed entry appears
- **WHEN** the EdgeBacktestService emits a progress event
- **THEN** the view SHALL append the entry to the corresponding tab's content and update metrics immediately

#### Scenario: Charts update progressively
- **WHEN** new entries stream in during the Charts tab view
- **THEN** equity curves and bar charts SHALL redraw to include the new data

### Requirement: R8 — Navigation and shortcuts
The view SHALL be accessible via `Ctrl+E`, return to dashboard via `Escape`, and implement `IShortcutHelpProvider` with entries for Ctrl+R (Run) and Escape (Return).

#### Scenario: Opening via Ctrl+E
- **WHEN** the user presses `Ctrl+E` on the dashboard
- **THEN** the system SHALL navigate to EdgeBacktestView

#### Scenario: Returning via Escape
- **WHEN** the user presses `Escape` in EdgeBacktestView
- **THEN** the system SHALL navigate back to the dashboard

#### Scenario: Shortcut help
- **WHEN** the keyboard shortcuts help is displayed
- **THEN** it SHALL include Ctrl+E (Edge Backtest), Ctrl+R (Run Backtest), and Escape (Return)

### Requirement: R9 — Symbol validation
The system SHALL validate entered symbols against `AppConfig.MarketFilter.IncludeKeywords` before starting a backtest.

#### Scenario: Valid symbols
- **WHEN** the user enters "BTC, ETH" and both are in IncludeKeywords
- **THEN** the backtest SHALL proceed

#### Scenario: Invalid symbol
- **WHEN** the user enters "INVALID_COIN"
- **THEN** the system SHALL display a validation error and NOT start the backtest

### Requirement: R10 — Progress indicator per timeframe
Each timeframe column SHALL display a progress indicator showing "Evaluating X/Y markets..." during the backtest.

#### Scenario: Progress display
- **WHEN** 5 of 20 markets have been evaluated for the left timeframe
- **THEN** the left column SHALL display "Evaluating 5/20 markets..."

#### Scenario: Completion
- **WHEN** all markets are evaluated for a timeframe
- **THEN** the progress indicator SHALL be replaced with the final results
