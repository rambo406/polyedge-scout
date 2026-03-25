namespace PolyEdgeScout.Console.Tests.ViewModels;

using NSubstitute;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.DTOs;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Console.ViewModels;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Tests for <see cref="EdgeBacktestViewModel"/>.
/// Verifies symbol validation, parsing, run lifecycle, and cancellation.
/// </summary>
public sealed class EdgeBacktestViewModelTests
{
    private readonly IEdgeBacktestService _service = Substitute.For<IEdgeBacktestService>();

    private static AppConfig CreateConfig() => new()
    {
        MarketFilter = new MarketFilterConfig
        {
            IncludeKeywords = ["bitcoin", "btc", "eth", "ethereum", "sol", "solana"]
        }
    };

    private EdgeBacktestViewModel CreateVm(
        AppConfig? config = null,
        params IEdgeFormula[] formulas)
    {
        config ??= CreateConfig();
        IEnumerable<IEdgeFormula> formulaList = formulas.Length > 0
            ? formulas
            : [CreateFormula("Default")];
        return new EdgeBacktestViewModel(_service, config, formulaList);
    }

    private static IEdgeFormula CreateFormula(string name)
    {
        var formula = Substitute.For<IEdgeFormula>();
        formula.Name.Returns(name);
        return formula;
    }

    /// <summary>
    /// ValidateSymbols returns symbols that don't match any include keyword.
    /// </summary>
    [Fact]
    public void ValidateSymbols_ReturnsInvalidSymbols()
    {
        var vm = CreateVm();
        vm.Symbols = "BTC,INVALID,ETH";

        var invalid = vm.ValidateSymbols();

        Assert.Single(invalid);
        Assert.Equal("INVALID", invalid[0]);
    }

    /// <summary>
    /// ValidateSymbols returns empty list when all symbols are valid.
    /// </summary>
    [Fact]
    public void ValidateSymbols_AllValid_ReturnsEmpty()
    {
        var vm = CreateVm();
        vm.Symbols = "BTC,ETH";

        var invalid = vm.ValidateSymbols();

        Assert.Empty(invalid);
    }

    /// <summary>
    /// ParseSymbols correctly splits a comma-separated string with whitespace.
    /// </summary>
    [Fact]
    public void ParseSymbols_SplitsCorrectly()
    {
        var vm = CreateVm();
        vm.Symbols = "BTC, ETH , SOL";

        var symbols = vm.ParseSymbols();

        Assert.Equal(3, symbols.Count);
        Assert.Equal("BTC", symbols[0]);
        Assert.Equal("ETH", symbols[1]);
        Assert.Equal("SOL", symbols[2]);
    }

    /// <summary>
    /// ParseSymbols handles empty entries correctly.
    /// </summary>
    [Fact]
    public void ParseSymbols_IgnoresEmptyEntries()
    {
        var vm = CreateVm();
        vm.Symbols = "BTC,,ETH,";

        var symbols = vm.ParseSymbols();

        Assert.Equal(2, symbols.Count);
        Assert.Equal("BTC", symbols[0]);
        Assert.Equal("ETH", symbols[1]);
    }

    /// <summary>
    /// RunEdgeBacktestAsync sets IsRunning during execution and clears it after completion.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktestAsync_SetsIsRunningDuringExecution()
    {
        var tcs = new TaskCompletionSource<EdgeBacktestResult>();
        _service.RunEdgeBacktestAsync(
                Arg.Any<List<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var vm = CreateVm();
        vm.Symbols = "BTC";
        var task = vm.RunEdgeBacktestAsync();

        Assert.True(vm.IsRunning);

        tcs.SetResult(new EdgeBacktestResult());
        await task;

        Assert.False(vm.IsRunning);
    }

    /// <summary>
    /// RunEdgeBacktestAsync fires BacktestCompleted event after completion.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktestAsync_FiresBacktestCompletedEvent()
    {
        _service.RunEdgeBacktestAsync(
                Arg.Any<List<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new EdgeBacktestResult
            {
                LeftTimeframeResult = new EdgeBacktestTimeframeResult { TotalMarkets = 10 },
                RightTimeframeResult = new EdgeBacktestTimeframeResult { TotalMarkets = 5 },
            });

        var vm = CreateVm();
        vm.Symbols = "BTC";
        bool completed = false;
        vm.BacktestCompleted += (_, _) => completed = true;

        await vm.RunEdgeBacktestAsync();

        Assert.True(completed);
        Assert.NotNull(vm.LeftTimeframeResult);
        Assert.Equal(10, vm.LeftTimeframeResult!.TotalMarkets);
    }

    /// <summary>
    /// CancelBacktest triggers cancellation — the service receives a cancelled token.
    /// </summary>
    [Fact]
    public async Task CancelBacktest_TriggersCancellation()
    {
        var tcs = new TaskCompletionSource<EdgeBacktestResult>();
        CancellationToken capturedToken = default;

        _service.RunEdgeBacktestAsync(
                Arg.Any<List<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedToken = ci.ArgAt<CancellationToken>(3);
                return tcs.Task;
            });

        var vm = CreateVm();
        vm.Symbols = "BTC";
        var task = vm.RunEdgeBacktestAsync();

        Assert.True(vm.IsRunning);

        // Cancel the backtest
        vm.CancelBacktest();

        Assert.True(capturedToken.IsCancellationRequested);

        // Complete the task to clean up
        tcs.SetResult(new EdgeBacktestResult());
        await task;

        Assert.False(vm.IsRunning);
    }

    /// <summary>
    /// RunEdgeBacktestAsync is no-op when already running.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktestAsync_WhenAlreadyRunning_IsNoop()
    {
        var tcs = new TaskCompletionSource<EdgeBacktestResult>();
        _service.RunEdgeBacktestAsync(
                Arg.Any<List<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var vm = CreateVm();
        vm.Symbols = "BTC";
        var task1 = vm.RunEdgeBacktestAsync();
        var task2 = vm.RunEdgeBacktestAsync(); // Should be no-op

        // Should complete immediately since IsRunning guard returns early
        await task2;

        Assert.True(vm.IsRunning);

        tcs.SetResult(new EdgeBacktestResult());
        await task1;
    }

    /// <summary>
    /// RunEdgeBacktestAsync skips execution when symbols are invalid.
    /// </summary>
    [Fact]
    public async Task RunEdgeBacktestAsync_InvalidSymbols_DoesNotRun()
    {
        var vm = CreateVm();
        vm.Symbols = "INVALID_SYMBOL";

        await vm.RunEdgeBacktestAsync();

        await _service.DidNotReceive().RunEdgeBacktestAsync(
            Arg.Any<List<string>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// AvailableFormulas reflects the registered formula names.
    /// </summary>
    [Fact]
    public void AvailableFormulas_ReflectsRegisteredFormulas()
    {
        var f1 = CreateFormula("Base");
        var f2 = CreateFormula("Scaled");

        var vm = CreateVm(config: null, f1, f2);

        Assert.Equal(2, vm.AvailableFormulas.Count);
        Assert.Contains("Base", vm.AvailableFormulas);
        Assert.Contains("Scaled", vm.AvailableFormulas);
    }
}
