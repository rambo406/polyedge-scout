using PolyEdgeScout.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PolyEdgeScout.Services;

/// <summary>
/// Real-time TUI dashboard rendered with Spectre.Console.
/// Uses a three-thread architecture:
///   1. Scan loop — periodically fetches markets, runs the probability model, and auto-trades.
///   2. Input loop — reads console keys for hotkey navigation.
///   3. Render loop — clears and redraws the dashboard every second.
/// Thread-safe shared state is protected by a lock on <see cref="_displayLock"/>.
/// </summary>
public sealed class DashboardService
{
    private const int MaxMarketRows = 15;
    private const int MaxLogLines = 8;
    private const int MaxQuestionLength = 45;

    private readonly AppConfig _config;
    private readonly ScannerService _scanner;
    private readonly ProbabilityModelService _probModel;
    private readonly OrderService _orders;
    private readonly LogService _log;

    // ── Shared state (guarded by _displayLock) ──
    private List<MarketDisplayRow> _displayMarkets = [];
    private readonly object _displayLock = new();
    private volatile bool _isScanning;
    private DateTime _lastScanTime = DateTime.MinValue;
    private int _selectedRow;
    private volatile bool _running = true;
    private CancellationTokenSource _cts = new();

    /// <summary>
    /// Initializes the dashboard with all required services.
    /// </summary>
    public DashboardService(
        AppConfig config,
        ScannerService scanner,
        ProbabilityModelService probModel,
        OrderService orders,
        LogService log)
    {
        _config = config;
        _scanner = scanner;
        _probModel = probModel;
        _orders = orders;
        _log = log;
    }

    /// <summary>
    /// Starts the dashboard: kicks off background scanning, input handling,
    /// and the main render loop. Returns when the user presses Q or the token is cancelled.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _cts = linkedCts;
        CancellationToken linked = linkedCts.Token;

        Task scanTask = Task.Run(() => ScanLoopAsync(linked), linked);
        Task inputTask = Task.Run(() => HandleInputAsync(linked), linked);

        try
        {
            await RenderLoopAsync(linked);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path
        }

        _running = false;

        try
        {
            await Task.WhenAll(scanTask, inputTask);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Background scan loop
    // ─────────────────────────────────────────────────────────────

    private async Task ScanLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _running)
        {
            _isScanning = true;
            try
            {
                List<Market> markets = await _scanner.ScanMarketsAsync(ct);
                var rows = new List<MarketDisplayRow>();

                foreach (Market market in markets)
                {
                    double modelProb = await _probModel.CalculateProbabilityAsync(market, ct);
                    double edge = modelProb - market.YesPrice;
                    string action = edge > _config.MinEdge && market.Volume < _config.MaxVolume
                        ? "🟢 BUY"
                        : "⚪ HOLD";

                    rows.Add(new MarketDisplayRow(market, modelProb, edge, action));

                    // Auto-trade when edge is sufficient
                    Trade? trade = _orders.EvaluateAndTrade(market, modelProb, ct);
                    if (trade is not null)
                    {
                        await _orders.ExecuteTradeAsync(trade, ct);
                    }
                }

                lock (_displayLock)
                {
                    _displayMarkets = rows.OrderByDescending(r => r.Edge).ToList();
                }

                _lastScanTime = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.Error("Scan cycle failed", ex);
            }
            finally
            {
                _isScanning = false;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.ScanIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Hotkey input handler
    // ─────────────────────────────────────────────────────────────

    private async Task HandleInputAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _running)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        _running = false;
                        await _cts.CancelAsync();
                        return;

                    case ConsoleKey.R:
                        _log.Info("Manual refresh triggered");
                        break;

                    case ConsoleKey.T:
                        _orders.PaperMode = !_orders.PaperMode;
                        _log.Info($"Toggled to {(_orders.PaperMode ? "PAPER" : "🔴 LIVE")} mode");
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
                            int max = Math.Max(0, _displayMarkets.Count - 1);
                            _selectedRow = Math.Min(max, _selectedRow + 1);
                        }
                        break;

                    case ConsoleKey.Enter:
                        ExecuteSelectedTrade();
                        break;
                }
            }

            try
            {
                await Task.Delay(50, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Main render loop
    // ─────────────────────────────────────────────────────────────

    private async Task RenderLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _running)
        {
            try
            {
                AnsiConsole.Clear();
                RenderDashboard();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Swallow render errors to keep dashboard alive
            }

            await Task.Delay(1000, ct);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Dashboard composition
    // ─────────────────────────────────────────────────────────────

    private void RenderDashboard()
    {
        // ── 1. Header ──
        IRenderable header = BuildHeader();

        // ── 2. Market scanner table ──
        IRenderable marketPanel = BuildMarketPanel();

        // ── 3. Portfolio + Last Trades (side-by-side) ──
        IRenderable portfolioRow = BuildPortfolioRow();

        // ── 4. Log panel ──
        IRenderable logPanel = BuildLogPanel();

        // ── 5. Hotkey footer ──
        IRenderable footer = BuildFooter();

        // ── Compose layout ──
        AnsiConsole.Write(header);
        AnsiConsole.Write(marketPanel);
        AnsiConsole.Write(portfolioRow);
        AnsiConsole.Write(logPanel);
        AnsiConsole.Write(footer);
    }

    // ─────────────────────────────────────────────────────────────
    //  Individual panel builders
    // ─────────────────────────────────────────────────────────────

    private IRenderable BuildHeader()
    {
        string mode = _orders.PaperMode ? "[grey]PAPER MODE[/]" : "[bold red]🔴 LIVE MODE[/]";
        string status = _isScanning ? "[yellow]Scanning...[/]" : "[green]Idle[/]";
        string lastScan = _lastScanTime == DateTime.MinValue
            ? "never"
            : $"{(DateTime.UtcNow - _lastScanTime).TotalSeconds:F0}s ago";

        string headerText =
            $"[bold white]🔍 PolyEdge Scout v1.0[/]\n" +
            $"{mode}  {status}  Last scan: {lastScan}";

        return new Panel(new Markup(headerText))
            .Border(BoxBorder.Double)
            .BorderColor(Color.Blue)
            .Expand();
    }

    private IRenderable BuildMarketPanel()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold cyan]MARKET SCANNER[/]")
            .Expand()
            .AddColumn(new TableColumn("[bold]#[/]").Width(3))
            .AddColumn(new TableColumn("[bold]Market Question[/]").Width(48))
            .AddColumn(new TableColumn("[bold]YES[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Volume[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Model[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Edge[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Action[/]").Centered());

        List<MarketDisplayRow> snapshot;
        int selected;

        lock (_displayLock)
        {
            snapshot = _displayMarkets.Take(MaxMarketRows).ToList();
            selected = _selectedRow;
        }

        if (snapshot.Count == 0)
        {
            table.AddRow("—", "[grey]No markets scanned yet. Waiting for first scan cycle...[/]",
                "", "", "", "", "");
        }
        else
        {
            for (int i = 0; i < snapshot.Count; i++)
            {
                MarketDisplayRow row = snapshot[i];
                bool isSelected = i == selected;

                string idx = isSelected ? $"[bold yellow]►{i + 1}[/]" : $" {i + 1}";
                string question = Markup.Escape(Truncate(row.Market.Question, MaxQuestionLength));
                question = isSelected ? $"[bold yellow]{question}[/]" : question;

                string yesPrice = $"{row.Market.YesPrice:F2}";
                string volume = $"${row.Market.Volume:N0}";

                string modelProb = $"{row.ModelProb:F2}";

                string edge = FormatEdge(row.Edge);
                string action = FormatAction(row.Action);

                table.AddRow(idx, question, yesPrice, volume, modelProb, edge, action);
            }
        }

        return table;
    }

    private IRenderable BuildPortfolioRow()
    {
        PnlSnapshot pnl = _orders.GetPnlSnapshot();

        // ── Left: Portfolio summary ──
        var portfolioTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold green]PORTFOLIO[/]")
            .HideHeaders()
            .AddColumn(new TableColumn("Label"))
            .AddColumn(new TableColumn("Value").RightAligned());

        portfolioTable.AddRow("[bold]Bankroll[/]", $"[white]${pnl.Bankroll:N2}[/]");
        portfolioTable.AddRow("[bold]Open Positions[/]", $"[white]{pnl.OpenPositions}[/]");
        portfolioTable.AddRow("[bold]Unrealized P&L[/]", FormatPnl(pnl.UnrealizedPnl));
        portfolioTable.AddRow("[bold]Realized P&L[/]", FormatPnl(pnl.RealizedPnl));
        portfolioTable.AddRow("[bold]Total P&L[/]", FormatPnl(pnl.TotalPnl));

        // ── Right: Last 5 trades ──
        var tradesTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold magenta]LAST 5 TRADES[/]")
            .AddColumn(new TableColumn("[bold]Market[/]"))
            .AddColumn(new TableColumn("[bold]Net[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]ROI[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Result[/]").Centered());

        if (pnl.LastTrades.Count == 0)
        {
            tradesTable.AddRow("[grey]No trades settled yet.[/]", "", "", "");
        }
        else
        {
            foreach (TradeResult trade in pnl.LastTrades)
            {
                string name = Markup.Escape(Truncate(trade.MarketQuestion, 30));
                string net = FormatPnl(trade.NetProfit);
                string roi = $"{trade.Roi:P0}";
                string result = trade.Won ? "[green]✅[/]" : "[red]❌[/]";

                tradesTable.AddRow(name, net, roi, result);
            }
        }

        // Side-by-side using Columns
        return new Columns(
            new Padder(portfolioTable, new Padding(0, 0, 1, 0)),
            new Padder(tradesTable, new Padding(1, 0, 0, 0)));
    }

    private IRenderable BuildLogPanel()
    {
        IReadOnlyList<string> messages = _log.RecentMessages;

        var lines = messages
            .TakeLast(MaxLogLines)
            .Select(m => Markup.Escape(m))
            .ToList();

        string content = lines.Count > 0
            ? string.Join("\n", lines)
            : "[grey]No log messages yet.[/]";

        return new Panel(new Markup(content))
            .Header("[bold blue]LOG[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Expand();
    }

    private static IRenderable BuildFooter()
    {
        string hotkeys =
            "[bold white][[R]][/] Refresh  " +
            "[bold white][[T]][/] Toggle Paper/Live  " +
            "[bold white][[Enter]][/] Trade Selected  " +
            "[bold white][[↑↓]][/] Navigate  " +
            "[bold white][[Q]][/] Quit";

        return new Panel(new Markup(hotkeys))
            .Border(BoxBorder.Heavy)
            .BorderColor(Color.DarkCyan)
            .Expand();
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private void ExecuteSelectedTrade()
    {
        MarketDisplayRow? row;

        lock (_displayLock)
        {
            if (_selectedRow < 0 || _selectedRow >= _displayMarkets.Count)
                return;

            row = _displayMarkets[_selectedRow];
        }

        if (row.Action != "🟢 BUY")
        {
            _log.Warn($"Selected market has no BUY signal: {Truncate(row.Market.Question, 60)}");
            return;
        }

        Trade? trade = _orders.EvaluateAndTrade(row.Market, row.ModelProb, CancellationToken.None);

        if (trade is null)
        {
            _log.Warn("Trade evaluation returned null for selected market.");
            return;
        }

        // Fire and forget — the trade executes asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _orders.ExecuteTradeAsync(trade, CancellationToken.None);
                _log.Info($"Manual trade executed: {Truncate(trade.MarketQuestion, 50)}");
            }
            catch (Exception ex)
            {
                _log.Error("Manual trade failed", ex);
            }
        });
    }

    private static string FormatEdge(double edge) => edge switch
    {
        > 0.08 => $"[bold green]+{edge:F2}[/]",
        > 0    => $"[yellow]+{edge:F2}[/]",
        _      => $"[red]{edge:F2}[/]",
    };

    private static string FormatAction(string action) => action switch
    {
        "🟢 BUY" => "[bold green]🟢 BUY[/]",
        _        => "[grey]⚪ HOLD[/]",
    };

    private static string FormatPnl(double value) => value switch
    {
        >= 0 => $"[green]+${value:N2}[/]",
        _    => $"[red]-${Math.Abs(value):N2}[/]",
    };

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : string.Concat(text.AsSpan(0, max - 1), "…");

    // ─────────────────────────────────────────────────────────────
    //  Internal display DTO
    // ─────────────────────────────────────────────────────────────

    private sealed record MarketDisplayRow(
        Market Market,
        double ModelProb,
        double Edge,
        string Action);
}
