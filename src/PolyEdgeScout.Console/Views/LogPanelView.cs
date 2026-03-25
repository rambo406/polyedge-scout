namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using System.Collections.ObjectModel;
using PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Terminal.Gui view for the log panel.
/// </summary>
public sealed class LogPanelView : FrameView
{
    private readonly LogViewModel _vm;
    private readonly ListView _listView;
    private readonly List<string> _items = [];

    public LogPanelView(LogViewModel vm) : base()
    {
        Title = "Log";
        _vm = vm;

        _listView = new ListView
        {
            Source = new ListWrapper<string>(new ObservableCollection<string>(_items)),
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        Add(_listView);

        _vm.MessageAdded += OnMessageAdded;
    }

    private void OnMessageAdded()
    {
        App?.Invoke(() =>
        {
            _items.Clear();
            // Show last 50 messages (most recent at bottom)
            var messages = _vm.Messages;
            var start = Math.Max(0, messages.Count - 50);
            for (var i = start; i < messages.Count; i++)
                _items.Add(messages[i]);

            _listView.Source = new ListWrapper<string>(new ObservableCollection<string>(_items));

            // Auto-scroll to bottom
            if (_items.Count > 0)
                _listView.SelectedItem = _items.Count - 1;
        });
    }
}
