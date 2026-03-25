namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Displays the most recent error message as a persistent indicator bar.
/// Hidden when no errors are active.
/// </summary>
public sealed class ErrorIndicatorView : Label
{
    private readonly DashboardViewModel _vm;

    public ErrorIndicatorView(DashboardViewModel vm) : base()
    {
        _vm = vm;
        Visible = false;
        Height = 1;
        Width = Dim.Fill();
        SetScheme(new Scheme
        {
            Normal = new Attribute(Color.White, Color.Red)
        });

        _vm.ErrorChanged += OnErrorChanged;
    }

    private void OnErrorChanged()
    {
        App?.Invoke(() =>
        {
            if (_vm.LastError is not null)
            {
                Text = $" ⚠ {_vm.LastError}";
                Visible = true;
            }
            else
            {
                Text = "";
                Visible = false;
            }
            SuperView?.SetNeedsDraw();
        });
    }
}
