namespace PolyEdgeScout.Console.Tests.ViewModels;

using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Domain.Entities;
using Xunit;

public class PortfolioViewModelTests
{
    [Fact]
    public void UpdateSnapshot_SetsSnapshotAndFiresEvent()
    {
        var vm = new PortfolioViewModel();
        var fired = false;
        vm.SnapshotUpdated += () => fired = true;

        var snapshot = new PnlSnapshot { Bankroll = 9500, OpenPositions = 3 };
        vm.UpdateSnapshot(snapshot);

        Assert.True(fired);
        Assert.Equal(9500, vm.Snapshot.Bankroll);
        Assert.Equal(3, vm.Snapshot.OpenPositions);
    }

    [Fact]
    public void DefaultSnapshot_IsEmpty()
    {
        var vm = new PortfolioViewModel();
        Assert.Equal(0, vm.Snapshot.Bankroll);
    }
}
