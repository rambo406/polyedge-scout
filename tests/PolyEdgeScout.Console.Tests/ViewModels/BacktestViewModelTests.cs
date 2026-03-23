namespace PolyEdgeScout.Console.Tests.ViewModels;

using NSubstitute;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Console.ViewModels;
using Xunit;

public class BacktestViewModelTests
{
    [Fact]
    public async Task RunBacktestAsync_SetsResultsAndFiresEvent()
    {
        var service = Substitute.For<IBacktestService>();
        var result = new BacktestResult { TotalMarkets = 50, BrierScore = 0.18 };
        service.RunBacktestAsync(Arg.Any<CancellationToken>()).Returns(result);

        var vm = new BacktestViewModel(service);
        var fired = false;
        vm.BacktestCompleted += () => fired = true;

        await vm.RunBacktestAsync();

        Assert.True(fired);
        Assert.NotNull(vm.Results);
        Assert.Equal(50, vm.Results!.TotalMarkets);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task RunBacktestAsync_SetsIsRunning()
    {
        var service = Substitute.For<IBacktestService>();
        var tcs = new TaskCompletionSource<BacktestResult>();
        service.RunBacktestAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);

        var vm = new BacktestViewModel(service);
        var task = vm.RunBacktestAsync();

        Assert.True(vm.IsRunning);

        tcs.SetResult(new BacktestResult());
        await task;

        Assert.False(vm.IsRunning);
    }
}
