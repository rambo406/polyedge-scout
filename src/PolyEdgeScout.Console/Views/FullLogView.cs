namespace PolyEdgeScout.Console.Views;

using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using System.Collections.ObjectModel;
using PolyEdgeScout.Console.App;
using PolyEdgeScout.Console.ViewModels;

/// <summary>
/// Full-screen view displaying all log entries across every level.
/// </summary>
public sealed class FullLogView : FrameView, IShortcutHelpProvider
{
    private readonly FullLogViewModel _vm;
    private readonly ListView _listView;
    private readonly TextView _textView;
    private readonly List<string> _items = [];

    /// <summary>
    /// Gets or sets the view navigator for switching back to the dashboard.
    /// </summary>
    public ViewNavigator? Navigator { get; set; }

    public FullLogView(FullLogViewModel vm) : base()
    {
        Title = "Log [Wrap: OFF]";
        Visible = false;
        _vm = vm;

        _listView = new ListView
        {
            Source = new ListWrapper<string>(new ObservableCollection<string>(_items)),
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _textView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            Visible = false,
            Text = string.Empty
        };

        Add(_listView);
        Add(_textView);

        _vm.EntryAdded += OnEntryAdded;
        _vm.WordWrapChanged += OnWordWrapChanged;
    }

    /// <inheritdoc />
    protected override bool OnKeyDown(Key key)
    {
        if (key == Key.Esc)
        {
            Navigator?.ShowDashboard();
            return true;
        }

        if (key == Key.W.WithCtrl)
        {
            _vm.ToggleWordWrap();
            return true;
        }

        if (key == Key.C.WithCtrl.WithShift)
        {
            CopyAllEntries();
            return true;
        }

        if (key == Key.C.WithCtrl)
        {
            CopySelectedEntry();
            return true;
        }

        return base.OnKeyDown(key);
    }

    private void CopySelectedEntry()
    {
        if (_listView.SelectedItem is not { } selectedIndex)
            return;

        var text = _vm.GetSelectedEntryText(selectedIndex);
        if (text is null)
            return;

        var success = App?.Clipboard?.TrySetClipboardData(text) ?? false;
        FlashTitle(success ? "Log [Copied!]" : "Log [Copy failed]");
    }

    private void CopyAllEntries()
    {
        var text = _vm.GetAllEntriesText();
        if (text is null)
            return;

        var success = App?.Clipboard?.TrySetClipboardData(text) ?? false;
        FlashTitle(success ? "Log [Copied!]" : "Log [Copy failed]");
    }

    private void FlashTitle(string flashText)
    {
        var normalTitle = GetNormalTitle();
        Title = flashText;
        App?.AddTimeout(TimeSpan.FromSeconds(2), () =>
        {
            Title = normalTitle;
            return false;
        });
    }

    private string GetNormalTitle() =>
        _vm.WordWrap ? "Log [Wrap: ON]" : "Log [Wrap: OFF]";

    private void OnEntryAdded()
    {
        App?.Invoke(() =>
        {
            _items.Clear();
            foreach (var entry in _vm.Entries)
                _items.Add(entry.Message);

            _listView.Source = new ListWrapper<string>(new ObservableCollection<string>(_items));

            if (_items.Count > 0)
                _listView.SelectedItem = _items.Count - 1;

            if (_vm.WordWrap)
            {
                RefreshTextView();
            }
        });
    }

    private void OnWordWrapChanged()
    {
        App?.Invoke(() =>
        {
            Title = _vm.WordWrap ? "Log [Wrap: ON]" : "Log [Wrap: OFF]";
            RebuildView();
        });
    }

    /// <summary>
    /// Toggles between <see cref="ListView"/> and <see cref="TextView"/> based on the current word-wrap state.
    /// </summary>
    private void RebuildView()
    {
        if (_vm.WordWrap)
        {
            _listView.Visible = false;
            RefreshTextView();
            _textView.Visible = true;
        }
        else
        {
            _textView.Visible = false;
            _listView.Visible = true;
        }
    }

    private void RefreshTextView()
    {
        _textView.Text = string.Join(Environment.NewLine, _items);
        _textView.MoveEnd();
    }

    /// <inheritdoc />
    public IReadOnlyList<ShortcutHelpItem> GetShortcuts() =>
    [
        new("Escape", "Return to Dashboard"),
        new("Ctrl+W", "Toggle Word Wrap"),
        new("Ctrl+C", "Copy Selected Entry"),
        new("Ctrl+Shift+C", "Copy All Entries")
    ];
}
