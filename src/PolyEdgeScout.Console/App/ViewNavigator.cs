namespace PolyEdgeScout.Console.App;

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;

/// <summary>
/// Manages switching between full-page views (Dashboard, FullLog, TradesManagement, etc.).
/// Dashboard is a group of views; other views are single full-page views registered via <see cref="RegisterView"/>.
/// </summary>
public sealed class ViewNavigator
{
    private readonly View[] _dashboardViews;
    private readonly Dictionary<ActiveView, object> _fullPageViews = [];
    private readonly Dictionary<ActiveView, IShortcutHelpProvider?> _providers = [];

    private ActiveView _activeView = ActiveView.Dashboard;

    /// <summary>
    /// Initializes a new instance of <see cref="ViewNavigator"/>.
    /// </summary>
    /// <param name="dashboardViews">The dashboard child views to show/hide as a group.</param>
    public ViewNavigator(View[] dashboardViews)
    {
        _dashboardViews = dashboardViews;
    }

    /// <summary>
    /// Registers a full-page view and its optional shortcut help provider.
    /// If the view itself implements <see cref="IShortcutHelpProvider"/> and no explicit provider is given,
    /// the view is used as its own provider.
    /// </summary>
    /// <param name="viewId">The identifier for this view. Must not be <see cref="ActiveView.Dashboard"/>.</param>
    /// <param name="view">The full-page view to register.</param>
    /// <param name="provider">An optional explicit shortcut help provider for the view.</param>
    public void RegisterView(ActiveView viewId, View view, IShortcutHelpProvider? provider = null)
    {
        _fullPageViews[viewId] = view ?? throw new ArgumentNullException(nameof(view));
        if (provider is not null)
            _providers[viewId] = provider;
        else if (view is IShortcutHelpProvider p)
            _providers[viewId] = p;
    }

    /// <summary>
    /// Gets or sets the shortcut help provider for the dashboard view.
    /// Set this after construction to enable context-aware help.
    /// </summary>
    public IShortcutHelpProvider? DashboardProvider { get; set; }

    /// <summary>
    /// Gets the currently active view.
    /// </summary>
    public ActiveView CurrentView => _activeView;

    /// <summary>
    /// Gets the shortcut help provider for the currently active view.
    /// Returns the provider for the active full-page view, or the dashboard provider
    /// when the dashboard is visible.
    /// </summary>
    public IShortcutHelpProvider? ActiveProvider =>
        _activeView == ActiveView.Dashboard
            ? DashboardProvider
            : _providers.GetValueOrDefault(_activeView);

    /// <summary>
    /// Switches to the specified view, hiding all others.
    /// </summary>
    /// <param name="target">The view to show.</param>
    public void Show(ActiveView target)
    {
        var anyView = _fullPageViews.Values.Select(v => (View)v).FirstOrDefault()
            ?? (_dashboardViews.Length > 0 ? _dashboardViews[0] : null);

        anyView?.App?.Invoke(() =>
        {
            _activeView = target;

            // Hide all full-page views
            foreach (var v in _fullPageViews.Values.Cast<View>())
                v.Visible = false;

            if (target == ActiveView.Dashboard)
            {
                foreach (var v in _dashboardViews)
                    v.Visible = true;

                if (_dashboardViews.Length > 0)
                    _dashboardViews[0].SetFocus();
            }
            else if (_fullPageViews.TryGetValue(target, out var targetObj))
            {
                var targetView = (View)targetObj;

                // Hide dashboard views
                foreach (var v in _dashboardViews)
                    v.Visible = false;

                targetView.Visible = true;
                targetView.SetFocus();
            }
        });
    }

    /// <summary>
    /// Shows the dashboard views and hides all full-page views.
    /// </summary>
    public void ShowDashboard() => Show(ActiveView.Dashboard);

    /// <summary>
    /// Shows the full log view and hides the dashboard views.
    /// </summary>
    public void ShowFullLog() => Show(ActiveView.FullLog);
}
