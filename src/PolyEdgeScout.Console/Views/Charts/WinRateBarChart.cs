namespace PolyEdgeScout.Console.Views.Charts;

using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.App;

/// <summary>
/// Draws horizontal win-rate bars for formula comparison.
/// Bars are color-coded: green if &gt;55%, yellow 50–55%, red &lt;50%.
/// </summary>
/// <example>
/// <code>
/// var chart = new WinRateBarChart();
/// chart.SetData([("Formula1", 0.58), ("Formula2", 0.52)]);
/// </code>
/// </example>
public class WinRateBarChart : View
{
    private List<(string FormulaName, double WinRate)> _data = [];

    /// <summary>Block character used to draw the bars.</summary>
    private const char BarChar = '█';

    /// <summary>
    /// Sets the win rate data and triggers a redraw.
    /// </summary>
    /// <param name="data">List of formula names and their win rates (0.0–1.0).</param>
    public void SetData(List<(string FormulaName, double WinRate)> data)
    {
        _data = data;
        SetNeedsDraw();
    }

    /// <summary>
    /// Draws the horizontal bar chart within the available viewport.
    /// </summary>
    protected override bool OnDrawingContent(DrawContext? context)
    {
        if (_data.Count == 0)
        {
            DrawEmptyMessage();
            return true;
        }

        var viewport = Viewport;
        var maxLabelLen = _data.Max(d => d.FormulaName.Length);
        var labelWidth = Math.Min(maxLabelLen + 1, viewport.Width / 3);
        var barWidth = viewport.Width - labelWidth - 8; // Reserve space for percentage text

        if (barWidth < 5)
        {
            return true;
        }

        for (var i = 0; i < _data.Count && i < viewport.Height; i++)
        {
            var (name, winRate) = _data[i];

            // Draw label
            var label = name.Length > labelWidth ? name[..(labelWidth - 1)] + "…" : name.PadRight(labelWidth);
            Move(0, i);
            AddStr(label);

            // Determine bar color
            var color = winRate switch
            {
                > 0.55 => new Attribute(Color.Green, Color.Black),
                >= 0.50 => new Attribute(Color.Yellow, Color.Black),
                _ => new Attribute(Color.Red, Color.Black)
            };

            // Draw bar
            var filledWidth = (int)(winRate * barWidth);
            filledWidth = Math.Clamp(filledWidth, 0, barWidth);

            SetAttribute(color);
            Move(labelWidth, i);
            AddStr(new string(BarChar, filledWidth));

            // Reset color and draw percentage
            SetAttribute(new Attribute(Color.White, Color.Black));
            Move(labelWidth + filledWidth + 1, i);
            AddStr($"{winRate:P1}");
        }

        return true;
    }

    /// <summary>
    /// Draws a placeholder message when no data is available.
    /// </summary>
    private void DrawEmptyMessage()
    {
        var viewport = Viewport;
        var msg = "No win rate data. Press Ctrl+R to run.";
        var x = Math.Max(0, (viewport.Width - msg.Length) / 2);
        var y = viewport.Height / 2;
        Move(x, y);
        AddStr(msg);
    }
}
