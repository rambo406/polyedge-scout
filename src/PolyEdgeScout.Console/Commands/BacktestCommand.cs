namespace PolyEdgeScout.Console.Commands;

using SystemConsole = System.Console;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Domain.Interfaces;

public sealed class BacktestCommand
{
    private readonly BacktestViewModel _vm;
    private readonly ILogService _log;

    public BacktestCommand(BacktestViewModel vm, ILogService log)
    {
        _vm = vm;
        _log = log;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        SystemConsole.WriteLine();
        SystemConsole.WriteLine("╔══════════════════════════════════════╗");
        SystemConsole.WriteLine("║       PolyEdge Scout - Backtest      ║");
        SystemConsole.WriteLine("╚══════════════════════════════════════╝");
        SystemConsole.WriteLine();

        SystemConsole.Write("Running backtest...");
        await _vm.RunBacktestAsync(ct);
        SystemConsole.WriteLine(" Done!");
        SystemConsole.WriteLine();

        if (_vm.Results is null)
        {
            SystemConsole.WriteLine("ERROR: No backtest results returned.");
            return;
        }

        DisplayResults(_vm.Results);
    }

    private static void DisplayResults(BacktestResult result)
    {
        // Summary
        SystemConsole.WriteLine("┌─────────────────────────────────────┐");
        SystemConsole.WriteLine("│         Backtest Summary            │");
        SystemConsole.WriteLine("├──────────────────┬──────────────────┤");
        SystemConsole.WriteLine($"│ Total Markets    │ {result.TotalMarkets,16} │");
        SystemConsole.WriteLine($"│ Markets w/ Edge  │ {result.MarketsWithEdge,16} │");
        SystemConsole.WriteLine($"│ Brier Score      │ {result.BrierScore,16:F4} │");
        SystemConsole.WriteLine($"│ Win Rate         │ {result.WinRate,15:P1} │");
        SystemConsole.WriteLine($"│ Edge Accuracy    │ {result.EdgeAccuracy,15:P1} │");
        SystemConsole.WriteLine($"│ Hypothetical P&L │ {result.HypotheticalPnl,15:C2} │");
        SystemConsole.WriteLine("└──────────────────┴──────────────────┘");
        SystemConsole.WriteLine();

        // Calibration
        if (result.CalibrationBuckets.Count > 0)
        {
            SystemConsole.WriteLine("Calibration:");
            SystemConsole.WriteLine($"  {"Bucket",-12} {"Predicted",10} {"Actual",10} {"Count",6} {"Delta",8}");
            SystemConsole.WriteLine($"  {"------",-12} {"--------",10} {"------",10} {"-----",6} {"-----",8}");
            foreach (var b in result.CalibrationBuckets)
            {
                var delta = b.AverageActual - b.AveragePredicted;
                SystemConsole.WriteLine($"  {b.Range,-12} {b.AveragePredicted,10:P1} {b.AverageActual,10:P1} {b.Count,6} {delta,+8:+0.00;-0.00}");
            }
            SystemConsole.WriteLine();
        }

        // Market details
        if (result.Entries.Count > 0)
        {
            SystemConsole.WriteLine("Market Details (top 30):");
            SystemConsole.WriteLine($"  {"Market",-40} {"Model",6} {"Mkt",6} {"Edge",7} {"Actual",6} {"OK?",4}");
            SystemConsole.WriteLine($"  {"------",-40} {"-----",6} {"---",6} {"----",7} {"------",6} {"---",4}");
            foreach (var e in result.Entries.Take(30))
            {
                var question = e.MarketQuestion.Length > 38
                    ? e.MarketQuestion[..37] + "…"
                    : e.MarketQuestion;
                var icon = e.ModelCorrect ? "✓" : "✗";
                SystemConsole.WriteLine($"  {question,-40} {e.ModelProbability,6:F2} {e.MarketYesPrice,6:F2} {e.Edge,+7:+0.00;-0.00} {e.ActualOutcome,6:F0} {icon,4}");
            }
        }
    }
}
