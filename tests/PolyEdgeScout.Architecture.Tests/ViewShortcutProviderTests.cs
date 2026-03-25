using System.Text.RegularExpressions;
using PolyEdgeScout.Console.App;

namespace PolyEdgeScout.Architecture.Tests;

/// <summary>
/// Ensures views that handle keyboard shortcuts advertise them via <see cref="IShortcutHelpProvider"/>.
/// Uses source-code scanning rather than reflection because Terminal.Gui's
/// module initializer crashes in the test context (TypeInitializationException).
/// </summary>
public sealed class ViewShortcutProviderTests
{
    /// <summary>
    /// Any concrete view that overrides <c>OnKeyDown</c> (excluding Dialog subclasses)
    /// must implement <see cref="IShortcutHelpProvider"/> so its shortcuts are discoverable.
    /// </summary>
    [Fact]
    public void Views_WithOnKeyDownOverride_ShouldImplement_IShortcutHelpProvider()
    {
        var viewsDir = GetConsoleViewsSourceDirectory();
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(viewsDir, "*.cs"))
        {
            var content = File.ReadAllText(file);
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (!HasOnKeyDownOverride(content))
            {
                continue;
            }

            // Dialog subclasses handle OnKeyDown for standard dismiss behavior;
            // they are not required to implement IShortcutHelpProvider.
            if (ExtendsDialog(content))
            {
                continue;
            }

            if (!ImplementsIShortcutHelpProvider(content))
            {
                violations.Add(fileName);
            }
        }

        Assert.True(
            violations.Count == 0,
            $"The following views override OnKeyDown but do not implement IShortcutHelpProvider: " +
            $"{string.Join(", ", violations)}");
    }

    /// <summary>
    /// Sanity check: at least some view source files should override OnKeyDown
    /// so the test is meaningful and doesn't silently pass on zero matches.
    /// </summary>
    [Fact]
    public void ConsoleViews_ShouldHaveSourceFilesWithOnKeyDownOverride()
    {
        var viewsDir = GetConsoleViewsSourceDirectory();

        var filesWithOnKeyDown = Directory.GetFiles(viewsDir, "*.cs")
            .Where(f => HasOnKeyDownOverride(File.ReadAllText(f)))
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        Assert.NotEmpty(filesWithOnKeyDown);
    }

    private static bool HasOnKeyDownOverride(string content) =>
        Regex.IsMatch(content, @"override\s+\w+\s+OnKeyDown\b");

    private static bool ExtendsDialog(string content) =>
        Regex.IsMatch(content, @"class\s+\w+\s*:.*\bDialog\b");

    private static bool ImplementsIShortcutHelpProvider(string content) =>
        content.Contains("IShortcutHelpProvider");

    private static string GetConsoleViewsSourceDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0 || dir.GetFiles("*.sln").Length > 0)
            {
                var viewsDir = Path.Combine(dir.FullName, "src", "PolyEdgeScout.Console", "Views");
                Assert.True(
                    Directory.Exists(viewsDir),
                    $"Console Views directory not found at: {viewsDir}");
                return viewsDir;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not find solution directory. Ensure the test runs from within the solution tree.");
    }
}
