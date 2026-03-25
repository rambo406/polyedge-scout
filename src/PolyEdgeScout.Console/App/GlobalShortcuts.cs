namespace PolyEdgeScout.Console.App;

/// <summary>
/// Provides the global keyboard shortcuts that are available on all pages.
/// </summary>
public static class GlobalShortcuts
{
    /// <summary>
    /// Gets the global keyboard shortcuts available across all views.
    /// </summary>
    public static IReadOnlyList<ShortcutHelpItem> Items { get; } =
    [
        new("Ctrl+Q", "Quit"),
        new("F1", "Help")
    ];
}
