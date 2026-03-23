namespace PolyEdgeScout.Console.Tests.ViewModels;

using PolyEdgeScout.Console.ViewModels;
using Xunit;

public class ErrorLogViewModelTests
{
    [Fact]
    public void OnLogEntry_WithErrLevel_AddsEntry()
    {
        var vm = new ErrorLogViewModel();

        vm.OnLogEntry("ERR", "[12:00:00] Something failed");

        Assert.Single(vm.Entries);
        Assert.Equal("ERR", vm.Entries[0].Level);
        Assert.Equal("[12:00:00] Something failed", vm.Entries[0].Message);
    }

    [Fact]
    public void OnLogEntry_WithWrnLevel_AddsEntry()
    {
        var vm = new ErrorLogViewModel();

        vm.OnLogEntry("WRN", "[12:00:00] Something suspicious");

        Assert.Single(vm.Entries);
        Assert.Equal("WRN", vm.Entries[0].Level);
        Assert.Equal("[12:00:00] Something suspicious", vm.Entries[0].Message);
    }

    [Fact]
    public void OnLogEntry_WithInfLevel_IgnoresEntry()
    {
        var vm = new ErrorLogViewModel();

        vm.OnLogEntry("INF", "[12:00:00] All good");

        Assert.Empty(vm.Entries);
    }

    [Fact]
    public void OnLogEntry_WithDbgLevel_IgnoresEntry()
    {
        var vm = new ErrorLogViewModel();

        vm.OnLogEntry("DBG", "[12:00:00] Debug trace");

        Assert.Empty(vm.Entries);
    }

    [Fact]
    public void OnLogEntry_FiresEntryAddedEvent()
    {
        var vm = new ErrorLogViewModel();
        var fired = false;
        vm.EntryAdded += () => fired = true;

        vm.OnLogEntry("ERR", "[12:00:00] Failure occurred");

        Assert.True(fired);
    }

    [Fact]
    public void OnLogEntry_DoesNotFireEventForFilteredLevels()
    {
        var vm = new ErrorLogViewModel();
        var fired = false;
        vm.EntryAdded += () => fired = true;

        vm.OnLogEntry("INF", "[12:00:00] Info message");

        Assert.False(fired);
    }

    [Fact]
    public void OnLogEntry_BoundsBufferAt500()
    {
        var vm = new ErrorLogViewModel();

        for (var i = 0; i < 600; i++)
            vm.OnLogEntry("ERR", $"Error {i}");

        Assert.Equal(500, vm.Entries.Count);
        Assert.Equal("Error 599", vm.Entries[^1].Message);
        Assert.Equal("Error 100", vm.Entries[0].Message);
    }

    [Fact]
    public void Entries_IsReadOnly()
    {
        var vm = new ErrorLogViewModel();

        vm.OnLogEntry("ERR", "Test error");

        var entries = vm.Entries;
        Assert.IsAssignableFrom<IReadOnlyList<ErrorLogEntry>>(entries);
        Assert.Single(entries);
    }
}
