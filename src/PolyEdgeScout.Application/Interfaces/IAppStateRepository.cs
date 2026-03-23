namespace PolyEdgeScout.Application.Interfaces;

public interface IAppStateRepository
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value);
    Task<double> GetBankrollAsync();
    Task SetBankrollAsync(double bankroll);
}
