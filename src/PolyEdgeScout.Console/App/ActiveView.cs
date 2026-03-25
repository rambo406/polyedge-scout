namespace PolyEdgeScout.Console.App;

/// <summary>
/// Identifies which full-page view is currently active in the application.
/// </summary>
public enum ActiveView
{
    /// <summary>The main dashboard with market table, portfolio, trades, and log panels.</summary>
    Dashboard,

    /// <summary>The full-page unified log view showing all levels.</summary>
    FullLog,

    /// <summary>The full-page trades management view.</summary>
    TradesManagement,

    /// <summary>The full-page backtest view for strategy backtesting.</summary>
    Backtest,

    /// <summary>The full-page edge backtest view for multi-formula comparison.</summary>
    EdgeBacktest
}
