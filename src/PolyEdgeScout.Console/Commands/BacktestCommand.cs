namespace PolyEdgeScout.Console.Commands;

using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Interfaces;
using Spectre.Console;

public sealed class BacktestCommand
{
    private readonly IBacktestService _backtestService;
    private readonly ILogService _log;

    public BacktestCommand(IBacktestService backtestService, ILogService log)
    {
        _backtestService = backtestService;
        _log = log;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        AnsiConsole.Write(new FigletText("PolyEdge Scout").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[bold]Backtest Mode[/]");
        AnsiConsole.WriteLine();

        var result = await AnsiConsole.Status()
            .StartAsync("Running backtest...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                return await _backtestService.RunBacktestAsync(ct);
            });

        DisplayResults(result);
    }

    private void DisplayResults(BacktestResult result)
    {
        // Summary table
        var summaryTable = new Table()
            .Title("[bold cyan]Backtest Summary[/]")
            .Border(TableBorder.Double)
            .AddColumn("Metric")
            .AddColumn("Value");

        var brierColor = result.BrierScore < 0.20 ? "green" : result.BrierScore < 0.25 ? "yellow" : "red";
        summaryTable.AddRow("Total Markets", $"{result.TotalMarkets}");
        summaryTable.AddRow("Markets with Edge", $"{result.MarketsWithEdge}");
        summaryTable.AddRow("Brier Score", $"[{brierColor}]{result.BrierScore:F4}[/]");
        summaryTable.AddRow("Win Rate", $"{result.WinRate:P1}");
        summaryTable.AddRow("Edge Accuracy", $"{result.EdgeAccuracy:P1}");

        var pnlColor = result.HypotheticalPnl >= 0 ? "green" : "red";
        summaryTable.AddRow("Hypothetical P&L", $"[{pnlColor}]${result.HypotheticalPnl:N2}[/]");

        AnsiConsole.Write(summaryTable);
        AnsiConsole.WriteLine();

        // Calibration table
        if (result.CalibrationBuckets.Count > 0)
        {
            var calTable = new Table()
                .Title("[bold cyan]Calibration[/]")
                .Border(TableBorder.Rounded)
                .AddColumn("Bucket")
                .AddColumn("Predicted")
                .AddColumn("Actual")
                .AddColumn("Count")
                .AddColumn("Delta");

            foreach (var bucket in result.CalibrationBuckets)
            {
                var delta = bucket.AverageActual - bucket.AveragePredicted;
                var deltaColor = Math.Abs(delta) < 0.1 ? "green" : Math.Abs(delta) < 0.2 ? "yellow" : "red";
                calTable.AddRow(
                    bucket.Range,
                    $"{bucket.AveragePredicted:P1}",
                    $"{bucket.AverageActual:P1}",
                    $"{bucket.Count}",
                    $"[{deltaColor}]{delta:+0.00;-0.00}[/]"
                );
            }

            AnsiConsole.Write(calTable);
            AnsiConsole.WriteLine();
        }

        // Market details table
        if (result.Entries.Count > 0)
        {
            var detailTable = new Table()
                .Title("[bold cyan]Market Details[/]")
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("Market").Width(40))
                .AddColumn("Model")
                .AddColumn("Market")
                .AddColumn("Edge")
                .AddColumn("Actual")
                .AddColumn("Correct?");

            foreach (var entry in result.Entries.Take(30))
            {
                var question = entry.MarketQuestion.Length > 38
                    ? entry.MarketQuestion[..37] + "…"
                    : entry.MarketQuestion;
                var icon = entry.ModelCorrect ? "[green]✅[/]" : "[red]❌[/]";

                detailTable.AddRow(
                    Markup.Escape(question),
                    $"{entry.ModelProbability:F2}",
                    $"{entry.MarketYesPrice:F2}",
                    $"{entry.Edge:+0.00;-0.00}",
                    $"{entry.ActualOutcome:F0}",
                    icon
                );
            }

            AnsiConsole.Write(detailTable);
        }
    }
}
