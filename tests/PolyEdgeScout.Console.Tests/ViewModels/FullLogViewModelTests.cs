namespace PolyEdgeScout.Console.Tests.ViewModels;

using PolyEdgeScout.Console.ViewModels;
using Xunit;

public class FullLogViewModelTests
{
    [Fact]
    public void OnLogEntry_WithErrLevel_AddsEntry()
    {
        var vm = new FullLogViewModel();

        vm.OnLogEntry("ERR", "[12:00:00] Something failed");

        Assert.Single(vm.Entries);
        Assert.Equal("ERR", vm.Entries[0].Level);
        Assert.Equal("[12:00:00] Something failed", vm.Entries[0].Message);
    }

    [Fact]
    public void OnLogEntry_WithWrnLevel_AddsEntry()
    {
        var vm = new FullLogViewModel();

        vm.OnLogEntry("WRN", "[12:00:00] Something suspicious");

        Assert.Single(vm.Entries);
        Assert.Equal("WRN", vm.Entries[0].Level);
        Assert.Equal("[12:00:00] Something suspicious", vm.Entries[0].Message);
    }

    [Fact]
    public void OnLogEntry_WithInfLevel_AddsEntry()
    {
        var vm = new FullLogViewModel();

        vm.OnLogEntry("INF", "[12:00:00] All good");

        Assert.Single(vm.Entries);
        Assert.Equal("INF", vm.Entries[0].Level);
    }

    [Fact]
    public void OnLogEntry_WithDbgLevel_AddsEntry()
    {
        var vm = new FullLogViewModel();

        vm.OnLogEntry("DBG", "[12:00:00] Debug trace");

        Assert.Single(vm.Entries);
        Assert.Equal("DBG", vm.Entries[0].Level);
    }

    [Fact]
    public void OnLogEntry_AcceptsAllLevels()
    {
        var vm = new FullLogViewModel();

        vm.OnLogEntry("ERR", "Error");
        vm.OnLogEntry("WRN", "Warning");
        vm.OnLogEntry("INF", "Info");
        vm.OnLogEntry("DBG", "Debug");

        Assert.Equal(4, vm.Entries.Count);
    }

    [Fact]
    public void OnLogEntry_FiresEntryAddedEvent()
    {
        var vm = new FullLogViewModel();
        var fired = false;
        vm.EntryAdded += () => fired = true;

        vm.OnLogEntry("ERR", "[12:00:00] Failure occurred");

        Assert.True(fired);
    }

    [Fact]
    public void OnLogEntry_FiresEntryAddedForAllLevels()
    {
        var vm = new FullLogViewModel();
        var count = 0;
        vm.EntryAdded += () => count++;

        vm.OnLogEntry("ERR", "Error");
        vm.OnLogEntry("WRN", "Warning");
        vm.OnLogEntry("INF", "Info");
        vm.OnLogEntry("DBG", "Debug");

        Assert.Equal(4, count);
    }

    [Fact]
    public void OnLogEntry_BoundsBufferAt500()
    {
        var vm = new FullLogViewModel();

        for (var i = 0; i < 600; i++)
            vm.OnLogEntry("INF", $"Entry {i}");

        Assert.Equal(500, vm.Entries.Count);
        Assert.Equal("Entry 599", vm.Entries[^1].Message);
        Assert.Equal("Entry 100", vm.Entries[0].Message);
    }

    [Fact]
    public void Entries_IsReadOnly()
    {
        var vm = new FullLogViewModel();

        vm.OnLogEntry("ERR", "Test error");

        var entries = vm.Entries;
        Assert.IsAssignableFrom<IReadOnlyList<LogEntry>>(entries);
        Assert.Single(entries);
    }

    [Fact]
    public void WordWrap_DefaultsToFalse()
    {
        var vm = new FullLogViewModel();

        Assert.False(vm.WordWrap);
    }

    [Fact]
    public void ToggleWordWrap_FlipsValueToTrue()
    {
        var vm = new FullLogViewModel();

        vm.ToggleWordWrap();

        Assert.True(vm.WordWrap);
    }

    [Fact]
    public void ToggleWordWrap_FlipsValueBackToFalse()
    {
        var vm = new FullLogViewModel();

        vm.ToggleWordWrap();
        vm.ToggleWordWrap();

        Assert.False(vm.WordWrap);
    }

    [Fact]
    public void ToggleWordWrap_FiresWordWrapChangedEvent()
    {
        var vm = new FullLogViewModel();
        var fired = false;
        vm.WordWrapChanged += () => fired = true;

        vm.ToggleWordWrap();

        Assert.True(fired);
    }

    [Fact]
    public void GetSelectedEntryText_ValidIndex_ReturnsMessage()
    {
        var vm = new FullLogViewModel();
        vm.OnLogEntry("ERR", "[12:00:00] First error");
        vm.OnLogEntry("WRN", "[12:00:01] A warning");

        var result = vm.GetSelectedEntryText(1);

        Assert.Equal("[12:00:01] A warning", result);
    }

    [Fact]
    public void GetSelectedEntryText_NegativeIndex_ReturnsNull()
    {
        var vm = new FullLogViewModel();
        vm.OnLogEntry("ERR", "[12:00:00] Error");

        var result = vm.GetSelectedEntryText(-1);

        Assert.Null(result);
    }

    [Fact]
    public void GetSelectedEntryText_IndexBeyondCount_ReturnsNull()
    {
        var vm = new FullLogViewModel();
        vm.OnLogEntry("ERR", "[12:00:00] Error");

        var result = vm.GetSelectedEntryText(5);

        Assert.Null(result);
    }

    [Fact]
    public void GetSelectedEntryText_EmptyList_ReturnsNull()
    {
        var vm = new FullLogViewModel();

        var result = vm.GetSelectedEntryText(0);

        Assert.Null(result);
    }

    [Fact]
    public void GetAllEntriesText_MultipleEntries_ReturnsJoinedMessages()
    {
        var vm = new FullLogViewModel();
        vm.OnLogEntry("ERR", "Error one");
        vm.OnLogEntry("WRN", "Warning two");
        vm.OnLogEntry("INF", "Info three");

        var result = vm.GetAllEntriesText();

        var expected = string.Join(Environment.NewLine, "Error one", "Warning two", "Info three");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetAllEntriesText_SingleEntry_ReturnsSingleMessage()
    {
        var vm = new FullLogViewModel();
        vm.OnLogEntry("ERR", "Only error");

        var result = vm.GetAllEntriesText();

        Assert.Equal("Only error", result);
    }

    [Fact]
    public void GetAllEntriesText_EmptyList_ReturnsNull()
    {
        var vm = new FullLogViewModel();

        var result = vm.GetAllEntriesText();

        Assert.Null(result);
    }
}
