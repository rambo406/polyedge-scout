## ADDED Requirements

### Requirement: Terminal.Gui event-driven keyboard handling replaces polling
All keyboard input SHALL be handled by Terminal.Gui's built-in event routing system, not by polling `Console.ReadKey`. Views receive keyboard events and delegate to ViewModel commands.

#### Scenario: Keyboard events are processed by the main loop
- **WHEN** the user presses a key
- **THEN** Terminal.Gui's main loop SHALL process the key event
- **AND** route it to the focused View or a registered global key handler
- **AND** the View SHALL delegate the action to the appropriate ViewModel command
- **AND** no background thread SHALL poll for keyboard input

### Requirement: Global keyboard shortcuts via MenuBar accelerators wired to ViewModel commands
The following global keyboard shortcuts SHALL be available regardless of which View is focused. All shortcuts delegate to `DashboardViewModel` or `MarketTableViewModel` commands.

#### Scenario: Ctrl+Q quits the dashboard
- **WHEN** the user presses `Ctrl+Q`
- **THEN** `MenuBarFactory` SHALL route to `DashboardViewModel.QuitCommand`
- **AND** `QuitCommand` SHALL call `Application.RequestStop()` and cancel the scan loop

#### Scenario: F5 triggers a manual scan refresh
- **WHEN** the user presses `F5`
- **THEN** `MenuBarFactory` SHALL route to `DashboardViewModel.RefreshCommand`
- **AND** `RefreshCommand` SHALL signal the scan loop to skip the remaining wait interval
- **AND** `DashboardViewModel.ScanStatusChanged` SHALL fire with scanning status
- **AND** `LogViewModel.AddMessage()` SHALL record `Manual refresh triggered`

#### Scenario: Ctrl+T toggles paper/live mode
- **WHEN** the user presses `Ctrl+T`
- **THEN** `MenuBarFactory` SHALL route to `DashboardViewModel.ToggleModeCommand`
- **AND** `ToggleModeCommand` SHALL toggle `IOrderService.PaperMode` between true and false
- **AND** `DashboardViewModel.ModeChanged` SHALL fire
- **AND** `LogViewModel.AddMessage()` SHALL record the mode change

#### Scenario: Ctrl+E or Enter executes trade on selected market
- **WHEN** the user presses `Ctrl+E` (global) or `Enter` (when MarketTableView is focused)
- **AND** `MarketTableViewModel.SelectedIndex` corresponds to a valid market
- **THEN** `MarketTableViewModel.ExecuteTradeCommand()` SHALL be called
- **AND** `LogViewModel.AddMessage()` SHALL record the trade attempt and outcome
- **AND** `MarketTableViewModel.TradeExecuted` SHALL fire

### Requirement: Tab navigation between Views
The user SHALL be able to navigate focus between dashboard Views using Tab/Shift+Tab.

#### Scenario: Tab cycles through focusable views
- **WHEN** the user presses `Tab`
- **THEN** focus SHALL move to the next focusable View in order: MarketTableView → PortfolioView → TradesView → LogPanelView → MarketTableView (cycle)
- **AND** the currently focused View SHALL have a visible focus indicator (highlighted border)

#### Scenario: Shift+Tab cycles in reverse
- **WHEN** the user presses `Shift+Tab`
- **THEN** focus SHALL move to the previous focusable View in reverse order

### Requirement: Market table keyboard navigation via MarketTableView
The MarketTableView SHALL support keyboard-driven row navigation, updating `MarketTableViewModel.SelectedIndex`.

#### Scenario: Arrow keys navigate rows
- **WHEN** the MarketTableView is focused and the user presses `↑`
- **THEN** `MarketTableViewModel.SelectedIndex` SHALL decrease by one
- **WHEN** the user presses `↓`
- **THEN** `MarketTableViewModel.SelectedIndex` SHALL increase by one

#### Scenario: Selection wraps or stops at boundaries
- **WHEN** the selected row is the first row and the user presses `↑`
- **THEN** the selection SHALL remain on the first row (no wrap)
- **WHEN** the selected row is the last row and the user presses `↓`
- **THEN** the selection SHALL remain on the last row (no wrap)

#### Scenario: Page Up/Page Down for fast navigation
- **WHEN** the user presses `Page Up` or `Page Down` while the market table is focused
- **THEN** the selection SHALL jump by one page of visible rows

#### Scenario: Home/End jump to first/last row
- **WHEN** the user presses `Home` while the market table is focused
- **THEN** the selection SHALL jump to the first row
- **WHEN** the user presses `End`
- **THEN** the selection SHALL jump to the last row

### Requirement: Log panel keyboard navigation via LogPanelView
The LogPanelView SHALL support keyboard-driven scrolling when focused, independent of `LogViewModel` state.

#### Scenario: Arrow keys scroll log content
- **WHEN** the LogPanelView is focused and the user presses `↑`
- **THEN** the log view SHALL scroll up by one line
- **WHEN** the user presses `↓`
- **THEN** the log view SHALL scroll down by one line

#### Scenario: End key returns to latest log
- **WHEN** the user presses `End` while the log panel is focused
- **THEN** the log view SHALL scroll to the bottom (most recent entry)
- **AND** auto-scroll SHALL resume for new entries from `LogViewModel.MessageAdded`

### Requirement: MenuBar keyboard activation
The MenuBar SHALL be accessible via keyboard (standard Terminal.Gui behavior).

#### Scenario: F9 or Alt activates the menu bar
- **WHEN** the user presses `F9` (Terminal.Gui default) or `Alt` key
- **THEN** the MenuBar SHALL become focused and highlighted
- **AND** the user SHALL be able to navigate menus with arrow keys
- **AND** pressing `Enter` SHALL activate the selected menu item (which calls the bound ViewModel command)
- **AND** pressing `Escape` SHALL close the menu and return focus to the previous View

## REMOVED Requirements

### Requirement: Console.ReadKey polling loop
The `HandleInputAsync` background thread that polled `Console.ReadKey` every 50ms is removed entirely. Terminal.Gui's event-driven input replaces it.
