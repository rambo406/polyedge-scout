namespace PolyEdgeScout.Console.Tests.ViewModels;

using NSubstitute;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Domain.Interfaces;
using Xunit;

public class LogViewModelTests
{
    [Fact]
    public void AddMessage_AppendsTimestampedMessageAndFiresEvent()
    {
        var logService = Substitute.For<ILogService>();
        var vm = new LogViewModel(logService);
        var fired = false;
        vm.MessageAdded += () => fired = true;

        vm.AddMessage("Test message");

        Assert.True(fired);
        Assert.Single(vm.Messages);
        Assert.Contains("Test message", vm.Messages[0]);
        Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\]", vm.Messages[0]); // has timestamp
    }

    [Fact]
    public void AddMessage_BoundsBuffer()
    {
        var logService = Substitute.For<ILogService>();
        var vm = new LogViewModel(logService);

        for (int i = 0; i < 600; i++)
            vm.AddMessage($"Msg {i}");

        Assert.Equal(500, vm.Messages.Count);
        Assert.Contains("Msg 599", vm.Messages[^1]); // last message preserved
    }

    [Fact]
    public void SyncFromLogService_LoadsExistingMessages()
    {
        var logService = Substitute.For<ILogService>();
        logService.RecentMessages.Returns(new List<string> { "old1", "old2" });
        var vm = new LogViewModel(logService);

        vm.SyncFromLogService();

        Assert.Equal(2, vm.Messages.Count);
        Assert.Equal("old2", vm.Messages[1]);
    }
}
