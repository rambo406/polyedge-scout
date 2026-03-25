namespace PolyEdgeScout.Console.Views.Charts;

using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.App;

/// <summary>
/// Draws multi-line ASCII equity curves for formula comparison.
/// Each formula is rendered with a distinct line character, scaled to the viewport.
/// X-axis represents market index, Y-axis represents cumulative P&amp;L.
/// </summary>
/// <example>
/// <code>
/// var chart = new EquityCurveChart();
/// chart.SetData([("Formula1", [0.5, 1.2, 0.8, 2.1])]);
/// </code>
/// </example>
public class EquityCurveChart : View
{
    private List<(string FormulaName, List<double> CumulativePnL)> _data = [];

    /// <summary>Characters used to distinguish different formula lines on the chart.</summary>
    private static readonly char[] LineChars = ['━', '─', '╌', '┄', '▪', '•'];

    /// <summary>
    /// Sets the equity curve data and triggers a redraw.
    /// </summary>
    /// <param name="data">List of formula names and their cumulative P&amp;L series.</param>
    public void SetData(List<(string FormulaName, List<double> CumulativePnL)> data)
    {
        _data = data;
        SetNeedsDraw();
    }

    /// <summary>
    /// Draws the equity curve chart within the available viewport.
    /// </summary>
    protected override bool OnDrawingContent(DrawContext? context)
    {
        if (_data.Count == 0)
        {
            DrawEmptyMessage();
            return true;
        }

        var viewport = Viewport;
        var chartHeight = viewport.Height - 2; // Reserve bottom row for X-axis labels and legend
        var chartWidth = viewport.Width - 8;   // Reserve left for Y-axis labels

        if (chartHeight < 3 || chartWidth < 5)
        {
            return true;
        }

        // Find global min/max across all formulas
        var allValues = _data.SelectMany(d => d.CumulativePnL).ToList();
        if (allValues.Count == 0)
        {
            return true;
        }

        var minVal = allValues.Min();
        var maxVal = allValues.Max();
        var range = maxVal - minVal;
        if (range == 0) range = 1;

        var maxPoints = _data.Max(d => d.CumulativePnL.Count);
        if (maxPoints == 0)
        {
            return true;
        }

        // Draw Y-axis labels
        for (var row = 0; row < chartHeight; row++)
        {
            var value = maxVal - (row * range / (chartHeight - 1));
            var label = value.ToString("N1").PadLeft(7);
            Move(0, row);
            AddStr(label);
            Move(7, row);
            AddStr("│");
        }

        // Draw X-axis line
        Move(7, chartHeight);
        AddStr("└" + new string('─', chartWidth));

        // Plot each formula
        for (var fi = 0; fi < _data.Count; fi++)
        {
            var (formulaName, points) = _data[fi];
            var lineChar = LineChars[fi % LineChars.Length];

            for (var pi = 0; pi < points.Count; pi++)
            {
                var x = (int)((double)pi / Math.Max(maxPoints - 1, 1) * (chartWidth - 1)) + 8;
                var y = (int)((maxVal - points[pi]) / range * (chartHeight - 1));
                y = Math.Clamp(y, 0, chartHeight - 1);

                if (x < viewport.Width)
                {
                    Move(x, y);
                    AddStr(lineChar.ToString());
                }
            }
        }

        // Draw legend at bottom
        var legendY = chartHeight + 1;
        if (legendY < viewport.Height)
        {
            var legendX = 1;
            for (var fi = 0; fi < _data.Count && legendX < viewport.Width - 10; fi++)
            {
                var lineChar = LineChars[fi % LineChars.Length];
                var label = $"{lineChar} {_data[fi].FormulaName}  ";
                Move(legendX, legendY);
                AddStr(label);
                legendX += label.Length;
            }
        }

        return true;
    }

    /// <summary>
    /// Draws a placeholder message when no data is available.
    /// </summary>
    private void DrawEmptyMessage()
    {
        var viewport = Viewport;
        var msg = "No equity data. Press Ctrl+R to run.";
        var x = Math.Max(0, (viewport.Width - msg.Length) / 2);
        var y = viewport.Height / 2;
        Move(x, y);
        AddStr(msg);
    }
}
