namespace PolyEdgeScout.Console.UI;

using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Interfaces;
using Spectre.Console;

public sealed class DashboardService
{
    private readonly AppConfig _config;
    private readonly IScannerService _scanner;
    private readonly IProbabilityModelService _probModel;
    private readonly IOrderService _orders;
    private readonly ILogService _log;

    // Thread-safe shared state
    private List<(Market Market, double ModelProb, double Edge, string Action)> _displayMarkets = [];
    private readonly object _displayLock = new();
    private volatile bool _isScanning;
    private DateTime _lastScanTime = DateTime.MinValue;
    private int _selectedRow;
    private volatile bool _running = true;
    private volatile bool _forceRefresh;

    public DashboardService(
        AppConfig config,
        IScannerService scanner,
        IProbabilityModelService probModel,
        IOrderService orders,
        ILogService log)
    {
        _config = config;
        _scanner = scanner;
        _probModel = probModel;
        _orders = orders;
        _log = log;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _log.Info("Dashboard starting...");

        // Three concurrent loops
        var scanTask = Task.Run(() => ScanLoopAsync(ct), ct);
        var inputTask = Task.Run(() => HandleInputAsync(ct), ct);

        // Main render loop (runs on this thread)
        await RenderLoopAsync(ct);

        // Wait for background tasks to finish
        try { await Task.WhenAll(scanTask, inputTask); }
        catch (OperationCanceledException) { }

        _log.Info("Dashboard stopped.");
    }

    private async Task ScanLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _running)
        {
            _isScanning = true;
            try
            {
                var markets = await _scanner.ScanMarketsAsync(ct);
                var displayItems = new List<(Market Market, double ModelProb, double Edge, string Action)>();

                foreach (var market in markets)
                {
                    if (ct.IsCancellationRequested) break;

                    var prob = await _probModel.CalculateProbabilityAsync(market, ct);
                    var edge = prob - market.YesPrice;
                    var action = edge > _config.MinEdge && market.Volume < _config.MaxVolume
                        ? "🟢 BUY" : "⚪ HOLD";
                    displayItems.Add((market, prob, edge, action));

                    // Auto-trade if edge is sufficient
                    var trade = _orders.EvaluateAndTrade(market, prob, ct);
                    if (trade is not null)
                    {
                        await _orders.ExecuteTradeAsync(trade, ct);
                    }
                }

                lock (_displayLock)
                {
                    _displayMarkets = displayItems.OrderByDescending(x => x.Edge).ToList();
                }
                _lastScanTime = DateTime.UtcNow;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _log.Error("Scan cycle failed", ex);
            }
            finally
            {
                _isScanning = false;
            }

            // Wait for interval or force refresh
            var elapsed = TimeSpan.Zero;
            var interval = TimeSpan.FromSeconds(_config.ScanIntervalSeconds);
            while (elapsed < interval && _running && !ct.IsCancellationRequested && !_forceRefresh)
            {
                await Task.Delay(250, ct);
                elapsed += TimeSpan.FromMilliseconds(250);
            }
            _forceRefresh = false;
        }
    }

    private async Task HandleInputAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _running)
        {
            if (System.Console.KeyAvailable)
            {
                var key = System.Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        _running = false;
                        break;
                    case ConsoleKey.R:
                        _forceRefresh = true;
                        _log.Info("Manual refresh triggered");
                        break;
                    case ConsoleKey.T:
                        _orders.PaperMode = !_orders.PaperMode;
                        _log.Info($"Mode toggled to {(_orders.PaperMode ? "PAPER" : "🔴 LIVE")}");
                        break;
                    case ConsoleKey.UpArrow:
                        lock (_displayLock)
                        {
                            _selectedRow = Math.Max(0, _selectedRow - 1);
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        lock (_displayLock)
                        {
                            _selectedRow = Math.Min(_displayMarkets.Count - 1, _selectedRow + 1);
                        }
                        break;
                    case ConsoleKey.Enter:
                        await ExecuteSelectedTradeAsync(ct);
                        break;
                }
            }
            await Task.Delay(50, ct);
        }
    }

    private async Task ExecuteSelectedTradeAsync(CancellationToken ct)
    {
        List<(Market Market, double ModelProb, double Edge, string Action)> snapshot;
        int row;
        lock (_displayLock)
        {
            snapshot = [.. _displayMarkets];
            row = _selectedRow;
        }

        if (row < 0 || row >= snapshot.Count) return;

        var (market, prob, edge, _) = snapshot[row];
        if (edge <= _config.MinEdge)
        {
            _log.Warn($"Insufficient edge for manual trade on: {market.Question}");
            return;
        }

        var trade = _orders.EvaluateAndTrade(market, prob, ct);
        if (trade is not null)
        {
            _log.Info($"Manual trade confirmed: {market.Question}");
            await _orders.ExecuteTradeAsync(trade, ct);
        }
    }

    private async Task RenderLoopAsync(CancellationToken ct)
    {
        // Hide cursor for cleaner display
        System.Console.CursorVisible = false;

        while (!ct.IsCancellationRequested && _running)
        {
            try
            {
                AnsiConsole.Clear();
                RenderDashboard();
            }
            catch (Exception) { /* Ignore render errors */ }

            try { await Task.Delay(1000, ct); }
            catch (OperationCanceledException) { break; }
        }

        System.Console.CursorVisible = true;
    }

    private void RenderDashboard()
    {
        // 1. HEADER
        var modeText = _orders.PaperMode ? "[green]PAPER MODE[/]" : "[red bold]🔴 LIVE MODE[/]";
        var statusText = _isScanning ? "[yellow]Scanning...[/]" : "[dim]Idle[/]";
        var lastScan = _lastScanTime == DateTime.MinValue
            ? "Never"
            : $"{(DateTime.UtcNow - _lastScanTime).TotalSeconds:N0}s ago";

        var header = new Panel(
            new Markup($"[bold cyan]🔍 PolyEdge Scout v1.0[/]  |  {modeText}  |  {statusText}  |  Last scan: {lastScan}"))
        {
            Border = BoxBorder.Double,
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(header);
        AnsiConsole.WriteLine();

        // 2. MARKET TABLE
        List<(Market Market, double ModelProb, double Edge, string Action)> markets;
        int selectedRow;
        lock (_displayLock)
        {
            markets = [.. _displayMarkets];
            selectedRow = _selectedRow;
        }

        var table = new Table()
            .Title("[bold]Market Scanner[/]")
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("#").Width(3))
            .AddColumn(new TableColumn("Market Question").Width(48))
            .AddColumn(new TableColumn("YES").Width(6).Centered())
            .AddColumn(new TableColumn("Volume").Width(10).RightAligned())
            .AddColumn(new TableColumn("Model").Width(6).Centered())
            .AddColumn(new TableColumn("Edge").Width(8).Centered())
            .AddColumn(new TableColumn("Action").Width(10).Centered());

        var maxRows = Math.Min(markets.Count, 15);
        for (var i = 0; i < maxRows; i++)
        {
            var (m, prob, edge, action) = markets[i];
            var isSelected = i == selectedRow;
            var prefix = isSelected ? "[bold yellow]" : "";
            var suffix = isSelected ? "[/]" : "";

            var question = Truncate(m.Question, 45);
            var edgeStr = edge > _config.MinEdge
                ? $"[green]{edge:+0.00;-0.00}[/]"
                : edge > 0 ? $"[yellow]{edge:+0.00;-0.00}[/]"
                : $"[red]{edge:+0.00;-0.00}[/]";
            var actionStr = action.Contains("BUY") ? $"[green]{action}[/]" : $"[dim]{action}[/]";

            table.AddRow(
                $"{prefix}{i + 1}{suffix}",
                $"{prefix}{Markup.Escape(question)}{suffix}",
                $"{prefix}{m.YesPrice:F2}{suffix}",
                $"{prefix}${m.Volume:N0}{suffix}",
                $"{prefix}{prob:F2}{suffix}",
                edgeStr,
                actionStr
            );
        }

        if (markets.Count == 0)
        {
            table.AddRow("", "[dim]No markets found yet — scanning...[/]", "", "", "", "", "");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // 3. PORTFOLIO + LAST TRADES (side by side)
        var pnl = _orders.GetPnlSnapshot();

        var portfolioTable = new Table()
            .Title("[bold]Portfolio[/]")
            .Border(TableBorder.Rounded)
            .HideHeaders()
            .AddColumn("Key")
            .AddColumn("Value");

        portfolioTable.AddRow("Bankroll", $"[bold]${pnl.Bankroll:N2}[/]");
        portfolioTable.AddRow("Open Positions", $"{pnl.OpenPositions}");
        portfolioTable.AddRow("Unrealized P&L", FormatPnl(pnl.UnrealizedPnl));
        portfolioTable.AddRow("Realized P&L", FormatPnl(pnl.RealizedPnl));
        portfolioTable.AddRow("Total P&L", FormatPnl(pnl.TotalPnl));

        var tradesTable = new Table()
            .Title("[bold]Last 5 Trades[/]")
            .Border(TableBorder.Rounded)
            .AddColumn("Market")
            .AddColumn("Profit")
            .AddColumn("ROI")
            .AddColumn("");

        if (pnl.LastTrades.Count > 0)
        {
            foreach (var t in pnl.LastTrades.Take(5))
            {
                var icon = t.Won ? "✅" : "❌";
                tradesTable.AddRow(
                    Markup.Escape(Truncate(t.MarketQuestion, 25)),
                    FormatPnl(t.NetProfit),
                    $"{t.Roi:P0}",
                    icon
                );
            }
        }
        else
        {
            tradesTable.AddRow("[dim]No trades yet[/]", "", "", "");
        }

        var columns = new Columns(portfolioTable, tradesTable);
        AnsiConsole.Write(columns);
        AnsiConsole.WriteLine();

        // 4. LOG PANEL
        var messages = _log.RecentMessages;
        var logLines = messages.Count > 0
            ? string.Join("\n", messages.TakeLast(8).Select(Markup.Escape))
            : "[dim]No log messages yet[/]";

        var logPanel = new Panel(new Markup(logLines))
        {
            Header = new PanelHeader("[bold]Log[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(logPanel);
        AnsiConsole.WriteLine();

        // 5. FOOTER
        var footer = new Panel(
            new Markup("[bold][R][/] Refresh  [bold][T][/] Toggle Paper/Live  [bold][Q][/] Quit  [bold][↑↓][/] Navigate  [bold][Enter][/] Trade"))
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(footer);
    }

    private static string FormatPnl(double value)
    {
        return value >= 0
            ? $"[green]+${value:N2}[/]"
            : $"[red]-${Math.Abs(value):N2}[/]";
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";
    }
}
