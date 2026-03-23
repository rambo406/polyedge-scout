namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.Views;
using PolyEdgeScout.Application.DTOs;

/// <summary>
/// ITableSource adapter for displaying market scan results in a TableView.
/// </summary>
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
