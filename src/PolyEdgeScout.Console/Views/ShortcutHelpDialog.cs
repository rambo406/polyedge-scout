namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using PolyEdgeScout.Console.App;

/// <summary>
/// Modal dialog that displays available keyboard shortcuts for the current view.
/// </summary>
public sealed class ShortcutHelpDialog : Dialog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShortcutHelpDialog"/> class.
    /// </summary>
    /// <param name="shortcuts">The keyboard shortcuts to display.</param>
    public ShortcutHelpDialog(IReadOnlyList<ShortcutHelpItem> shortcuts) : base()
    {
        Title = "Keyboard Shortcuts";
        Width = 50;
        Height = shortcuts.Count + 6; // padding for borders + title + empty line

        var lines = new List<string>();
        foreach (var item in shortcuts)
        {
            lines.Add($"  {item.Key,-14} {item.Description}");
        }

        var textView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = string.Join(Environment.NewLine, lines)
        };

        Add(textView);
    }

    /// <inheritdoc />
    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.F1)
        {
            App?.RequestStop(this);
            return true;
        }

        return base.OnKeyDown(key);
    }
}
