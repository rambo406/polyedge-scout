namespace PolyEdgeScout.Console.App;

/// <summary>
/// Provides keyboard shortcut definitions for the help overlay.
/// Views that handle keyboard shortcuts should implement this interface
/// to make their shortcuts discoverable via the F1 help dialog.
/// </summary>
public interface IShortcutHelpProvider
{
    /// <summary>
    /// Gets the keyboard shortcuts available in this view.
    /// </summary>
    IReadOnlyList<ShortcutHelpItem> GetShortcuts();
}
