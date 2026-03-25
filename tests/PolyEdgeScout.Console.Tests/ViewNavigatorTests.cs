namespace PolyEdgeScout.Console.Tests;

using System.Reflection;
using PolyEdgeScout.Console.App;

/// <summary>
/// Tests for <see cref="ViewNavigator"/> properties that don't require Application.Init().
/// Uses pure reflection to construct ViewNavigator with null View parameters,
/// avoiding JIT resolution of Terminal.Gui types whose module initializer
/// crashes in the test context (TypeInitializationException).
/// Show/ShowDashboard/ShowFullLog use Application.Invoke() and cannot be tested here.
/// </summary>
public sealed class ViewNavigatorTests
{
    private static readonly Type NavigatorType = typeof(ViewNavigator);
    private static readonly ConstructorInfo Ctor = NavigatorType.GetConstructors()[0];
    private static readonly PropertyInfo DashboardProp = NavigatorType.GetProperty("DashboardProvider")!;
    private static readonly PropertyInfo ActiveProp = NavigatorType.GetProperty("ActiveProvider")!;
    private static readonly PropertyInfo CurrentViewProp = NavigatorType.GetProperty("CurrentView")!;

    private sealed class StubProvider : IShortcutHelpProvider
    {
        public IReadOnlyList<ShortcutHelpItem> GetShortcuts() =>
            [new("Test", "Test shortcut")];
    }

    /// <summary>
    /// Creates a ViewNavigator via reflection with null View[] parameter
    /// to avoid triggering Terminal.Gui module initializer.
    /// </summary>
    private static object CreateNavigator() => Ctor.Invoke([null]);

    [Fact]
    public void ActiveProvider_DefaultsToNull_WhenDashboardProviderNotSet()
    {
        var navigator = CreateNavigator();

        Assert.Null(ActiveProp.GetValue(navigator));
    }

    [Fact]
    public void ActiveProvider_ReturnsDashboardProvider_WhenSet()
    {
        var navigator = CreateNavigator();
        var provider = new StubProvider();

        DashboardProp.SetValue(navigator, provider);

        Assert.Same(provider, ActiveProp.GetValue(navigator));
    }

    [Fact]
    public void DashboardProvider_DefaultsToNull()
    {
        var navigator = CreateNavigator();

        Assert.Null(DashboardProp.GetValue(navigator));
    }

    [Fact]
    public void DashboardProvider_CanBeSetAndRead()
    {
        var navigator = CreateNavigator();
        var provider = new StubProvider();

        DashboardProp.SetValue(navigator, provider);

        Assert.Same(provider, DashboardProp.GetValue(navigator));
    }

    [Fact]
    public void CurrentView_DefaultsToDashboard()
    {
        var navigator = CreateNavigator();

        var currentView = (ActiveView)CurrentViewProp.GetValue(navigator)!;

        Assert.Equal(ActiveView.Dashboard, currentView);
    }

    [Fact]
    public void CurrentView_DefaultsToDashboard_ActiveProviderIsNull_WhenNoDashboardProvider()
    {
        var navigator = CreateNavigator();

        var currentView = (ActiveView)CurrentViewProp.GetValue(navigator)!;

        Assert.Equal(ActiveView.Dashboard, currentView);
        Assert.Null(ActiveProp.GetValue(navigator));
    }

    [Fact]
    public void CurrentView_DefaultsToDashboard_ActiveProviderReturnsDashboardProvider()
    {
        var navigator = CreateNavigator();
        var provider = new StubProvider();
        DashboardProp.SetValue(navigator, provider);

        var currentView = (ActiveView)CurrentViewProp.GetValue(navigator)!;

        Assert.Equal(ActiveView.Dashboard, currentView);
        Assert.Same(provider, ActiveProp.GetValue(navigator));
    }
}
