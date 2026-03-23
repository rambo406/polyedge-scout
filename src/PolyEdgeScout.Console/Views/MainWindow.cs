namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

/// <summary>
/// Main Terminal.Gui window composing all child views.
/// </summary>
public sealed class MainWindow : Window
{
    public MainWindow(
        MarketTableView marketTable,
        PortfolioView portfolio,
        TradesView trades,
        LogPanelView logPanel,
        ErrorIndicatorView errorIndicator) : base()
    {
        Title = "PolyEdge Scout v1.0";

        // Error indicator at top, hidden by default
        errorIndicator.X = 0;
        errorIndicator.Y = 0;
        errorIndicator.Width = Dim.Fill();

        // Market table: top-left, 65% width, 60% height
        marketTable.X = 0;
        marketTable.Y = Pos.Bottom(errorIndicator);
        marketTable.Width = Dim.Percent(65);
        marketTable.Height = Dim.Percent(60);

        // Portfolio: top-right
        portfolio.X = Pos.Right(marketTable);
        portfolio.Y = Pos.Bottom(errorIndicator);
        portfolio.Width = Dim.Fill();
        portfolio.Height = Dim.Percent(30);

        // Trades: below portfolio, right side
        trades.X = Pos.Right(marketTable);
        trades.Y = Pos.Bottom(portfolio);
        trades.Width = Dim.Fill();
        trades.Height = Dim.Percent(30);

        // Log panel: bottom, full width, remaining height
        logPanel.X = 0;
        logPanel.Y = Pos.Bottom(marketTable);
        logPanel.Width = Dim.Fill();
        logPanel.Height = Dim.Fill();

        Add(errorIndicator, marketTable, portfolio, trades, logPanel);
    }
}
