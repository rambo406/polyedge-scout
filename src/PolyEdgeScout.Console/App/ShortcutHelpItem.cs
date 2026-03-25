namespace PolyEdgeScout.Console.App;

/// <summary>
/// Represents a single keyboard shortcut entry for the help overlay.
/// </summary>
/// <param name="Key">The keyboard shortcut display text (e.g., "Ctrl+W").</param>
/// <param name="Description">A brief description of what the shortcut does.</param>
public record ShortcutHelpItem(string Key, string Description);
