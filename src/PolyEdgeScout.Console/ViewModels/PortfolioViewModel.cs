namespace PolyEdgeScout.Console.ViewModels;

using PolyEdgeScout.Domain.Entities;

/// <summary>
/// ViewModel for the portfolio panel.
/// Holds the current PnlSnapshot and notifies Views when it changes.
/// </summary>
public sealed class PortfolioViewModel
{
    public PnlSnapshot Snapshot { get; private set; } = new();

    public event Action? SnapshotUpdated;

    public void UpdateSnapshot(PnlSnapshot snapshot)
    {
        Snapshot = snapshot;
        SnapshotUpdated?.Invoke();
    }
}
