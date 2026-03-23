using System.Numerics;
using System.Text;
using System.Text.Json;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.ABI.EIP712;
using PolyEdgeScout.Models;

namespace PolyEdgeScout.Services;

/// <summary>
/// Manages trade evaluation, execution (paper and live), and settlement.
/// Maintains an in-memory ledger of open and settled trades with thread-safe access.
/// </summary>
public sealed class OrderService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// EIP-712 domain name for Polymarket CLOB orders.
    /// </summary>
    private const string Eip712DomainName = "Polymarket CTF Exchange";

    /// <summary>
    /// EIP-712 domain version for Polymarket CLOB orders.
    /// </summary>
    private const string Eip712DomainVersion = "1";

    /// <summary>
    /// Polygon chain ID.
    /// </summary>
    private const int PolygonChainId = 137;

    /// <summary>
    /// Polymarket CTF Exchange contract address on Polygon.
    /// </summary>
    private const string ExchangeContractAddress = "0x4bFb41d5B3570DeFd03C39a9A4D8dE6Bd8B8982E";

    private readonly AppConfig _config;
    private readonly HttpClient _http;
    private readonly LogService _log;

    private readonly List<Trade> _openTrades = new();
    private readonly List<TradeResult> _settledTrades = new();
    private double _bankroll = 10_000.0;
    private bool _paperMode;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new <see cref="OrderService"/>.
    /// </summary>
    /// <param name="config">Application configuration.</param>
    /// <param name="httpClient">Shared HTTP client for CLOB API calls.</param>
    /// <param name="log">Logging service.</param>
    public OrderService(AppConfig config, HttpClient httpClient, LogService log)
    {
        _config = config;
        _http = httpClient;
        _log = log;
        _paperMode = config.PaperMode;
    }

    /// <summary>
    /// Gets a read-only snapshot of all currently open trades.
    /// </summary>
    public IReadOnlyList<Trade> OpenTrades
    {
        get
        {
            lock (_lock)
            {
                return _openTrades.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets a read-only snapshot of all settled trade results.
    /// </summary>
    public IReadOnlyList<TradeResult> SettledTrades
    {
        get
        {
            lock (_lock)
            {
                return _settledTrades.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets the current available bankroll in USD.
    /// </summary>
    public double Bankroll
    {
        get
        {
            lock (_lock)
            {
                return _bankroll;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the service operates in paper-trading mode.
    /// When <c>true</c>, no real orders are submitted on-chain.
    /// </summary>
    public bool PaperMode
    {
        get
        {
            lock (_lock)
            {
                return _paperMode;
            }
        }
        set
        {
            lock (_lock)
            {
                _paperMode = value;
                _log.Info($"Paper mode toggled to: {value}");
            }
        }
    }

    /// <summary>
    /// Returns a point-in-time snapshot of the portfolio's profit and loss.
    /// </summary>
    /// <returns>A <see cref="PnlSnapshot"/> with current bankroll, positions, and recent results.</returns>
    public PnlSnapshot GetPnlSnapshot()
    {
        lock (_lock)
        {
            double unrealized = _openTrades.Sum(t => (1.0 - t.EntryPrice) * t.Shares);
            double realized = _settledTrades.Sum(t => t.NetProfit);

            return new PnlSnapshot
            {
                Bankroll = _bankroll,
                OpenPositions = _openTrades.Count,
                UnrealizedPnl = unrealized,
                RealizedPnl = realized,
                LastTrades = _settledTrades
                    .OrderByDescending(t => t.SettledAt)
                    .Take(5)
                    .ToList(),
            };
        }
    }

    /// <summary>
    /// Evaluates whether a market presents a positive-edge opportunity and, if so,
    /// creates a sized <see cref="Trade"/> record. Does not execute the trade.
    /// </summary>
    /// <param name="market">The market to evaluate.</param>
    /// <param name="modelProbability">Model's estimated probability for the YES outcome.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Trade"/> if the edge is sufficient; otherwise <c>null</c> (hold).</returns>
    public Trade? EvaluateAndTrade(Market market, double modelProbability, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        double edge = modelProbability - market.YesPrice;

        if (edge <= _config.MinEdge)
        {
            _log.Debug($"HOLD — edge {edge:P2} below min {_config.MinEdge:P2}: {Truncate(market.Question)}");
            return null;
        }

        if (market.Volume >= _config.MaxVolume)
        {
            _log.Debug($"HOLD — volume {market.Volume:F0} exceeds max {_config.MaxVolume:F0}: {Truncate(market.Question)}");
            return null;
        }

        // Size the bet: minimum of flat bet and Kelly sizing for safety
        double flatBet = _config.DefaultBetSize;
        double kellyBet = MathHelper.KellyBetSize(
            modelProbability,
            market.YesPrice,
            _bankroll,
            _config.KellyFraction,
            _config.MaxBankrollPercent);

        double betSize = Math.Min(flatBet, kellyBet);

        if (betSize <= 0)
        {
            _log.Debug($"HOLD — Kelly sizing returned zero bet for: {Truncate(market.Question)}");
            return null;
        }

        double shares = betSize / market.YesPrice;

        var trade = new Trade
        {
            MarketQuestion = market.Question,
            ConditionId = market.ConditionId,
            TokenId = market.TokenId,
            Action = TradeAction.Buy,
            Status = TradeStatus.Pending,
            EntryPrice = market.YesPrice,
            Shares = shares,
            ModelProbability = modelProbability,
            Edge = edge,
            Outlay = betSize,
            IsPaper = _paperMode,
        };

        _log.Info(
            $"TRADE SIGNAL: {Truncate(market.Question)} | " +
            $"Edge={edge:P2} ModelP={modelProbability:P2} Price={market.YesPrice:F3} " +
            $"Bet=${betSize:F2} Shares={shares:F2}");

        return trade;
    }

    /// <summary>
    /// Executes a previously evaluated trade. In paper mode the trade is filled locally;
    /// in live mode a signed EIP-712 order is submitted to the Polymarket CLOB.
    /// </summary>
    /// <param name="trade">The trade to execute (must have status <see cref="TradeStatus.Pending"/>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The trade with updated status and, in live mode, a transaction hash.</returns>
    public async Task<Trade> ExecuteTradeAsync(Trade trade, CancellationToken ct)
    {
        if (_paperMode)
        {
            return ExecutePaperTrade(trade);
        }

        return await ExecuteLiveTradeAsync(trade, ct);
    }

    /// <summary>
    /// Settles an open trade after its underlying market resolves.
    /// Calculates P&amp;L, records the result, and adjusts the bankroll.
    /// </summary>
    /// <param name="tradeId">The ID of the open trade to settle.</param>
    /// <param name="won">Whether the YES outcome resolved in the trade's favour.</param>
    public void SettleTrade(string tradeId, bool won)
    {
        lock (_lock)
        {
            Trade? trade = _openTrades.FirstOrDefault(t => t.Id == tradeId);
            if (trade is null)
            {
                _log.Warn($"SettleTrade: trade {tradeId} not found in open trades.");
                return;
            }

            double grossProfit = won
                ? (trade.Shares * 1.0) - (trade.Shares * trade.EntryPrice)
                : -(trade.Shares * trade.EntryPrice);

            double fees = trade.Outlay * 0.02; // 2% fee estimate
            double gas = _paperMode ? 0.0 : 0.50; // estimated gas cost
            double netProfit = grossProfit - fees - gas;
            double roi = trade.Outlay > 0 ? netProfit / trade.Outlay : 0;

            var result = new TradeResult
            {
                TradeId = trade.Id,
                MarketQuestion = trade.MarketQuestion,
                EntryPrice = trade.EntryPrice,
                Shares = trade.Shares,
                GrossProfit = grossProfit,
                Fees = fees,
                Gas = gas,
                Roi = roi,
                SettledAt = DateTime.UtcNow,
                Won = won,
            };

            _settledTrades.Add(result);
            _openTrades.Remove(trade);

            if (won)
            {
                // Winning trade returns the full share value ($1 per share)
                _bankroll += trade.Shares * 1.0 - fees - gas;
            }
            else
            {
                // Loss — outlay was already deducted; only subtract remaining fees/gas
                _bankroll -= fees + gas;
            }

            _log.Info(
                $"SETTLED: {Truncate(trade.MarketQuestion)} | " +
                $"Won={won} Gross=${grossProfit:F2} Fees=${fees:F2} Gas=${gas:F2} " +
                $"Net=${result.NetProfit:F2} ROI={roi:P2} Bankroll=${_bankroll:F2}");
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Private helpers
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills a trade locally in paper-trading mode.
    /// </summary>
    private Trade ExecutePaperTrade(Trade trade)
    {
        Trade filled = trade with
        {
            Status = TradeStatus.Filled,
            IsPaper = true,
        };

        lock (_lock)
        {
            _openTrades.Add(filled);
            _bankroll -= filled.Outlay;
        }

        _log.Info(
            $"PAPER TRADE: {Truncate(filled.MarketQuestion)} | " +
            $"Price={filled.EntryPrice:F3} Shares={filled.Shares:F2} " +
            $"Outlay=${filled.Outlay:F2} Bankroll=${_bankroll:F2}");

        return filled;
    }

    /// <summary>
    /// Signs and submits a live order to the Polymarket CLOB via EIP-712.
    /// Uses Nethereum to sign the typed-data order structure.
    /// </summary>
    private async Task<Trade> ExecuteLiveTradeAsync(Trade trade, CancellationToken ct)
    {
        string? privateKey = Environment.GetEnvironmentVariable("PRIVATE_KEY");
        if (string.IsNullOrWhiteSpace(privateKey))
        {
            _log.Error("PRIVATE_KEY environment variable is not set. Cannot execute live trade.");
            throw new InvalidOperationException("PRIVATE_KEY environment variable is required for live trading.");
        }

        try
        {
            var ecKey = new EthECKey(privateKey);
            string walletAddress = ecKey.GetPublicAddress();
            _log.Info($"Signing order with wallet: {walletAddress}");

            // Build order parameters
            string salt = GenerateRandomSalt();
            BigInteger sharesBaseUnits = ToBaseUnits(trade.Shares, 6); // USDC has 6 decimals
            BigInteger costBaseUnits = ToBaseUnits(trade.Outlay, 6);

            // Build EIP-712 typed data JSON (MetaMask-style) for Polymarket CLOB order
            string tokenIdValue = trade.TokenId.Length > 0 ? trade.TokenId : "0";

            var eip712Json = BuildEip712Json(
                salt, walletAddress, tokenIdValue,
                sharesBaseUnits.ToString(), costBaseUnits.ToString());

            // Sign the EIP-712 typed data using Nethereum
            Eip712TypedDataSigner signer = new();
            string signature = signer.SignTypedDataV4<Domain>(eip712Json, ecKey, "Order");

            _log.Info($"EIP-712 signature generated: {signature[..20]}...");

            // Build the CLOB API request body
            var orderPayload = new
            {
                order = new
                {
                    salt,
                    maker = walletAddress,
                    signer = walletAddress,
                    taker = "0x0000000000000000000000000000000000000000",
                    tokenId = trade.TokenId,
                    makerAmount = sharesBaseUnits.ToString(),
                    takerAmount = costBaseUnits.ToString(),
                    side = "BUY",
                    expiration = "0",
                    nonce = "0",
                    feeRateBps = "0",
                    signatureType = 2,
                    signature,
                },
            };

            string jsonBody = JsonSerializer.Serialize(orderPayload, JsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            string clobUrl = $"{_config.ClobBaseUrl}/order";
            _log.Info($"Submitting order to CLOB: {clobUrl}");

            using HttpResponseMessage response = await _http.PostAsync(clobUrl, content, ct);
            string responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _log.Error($"CLOB order failed ({response.StatusCode}): {responseBody}");
                throw new HttpRequestException(
                    $"CLOB order submission failed with status {response.StatusCode}: {responseBody}");
            }

            // Extract order ID / tx hash from response
            string? txHash = ExtractOrderId(responseBody);
            _log.Info($"Order accepted — ID/TxHash: {txHash ?? "unknown"}");

            Trade filled = trade with
            {
                Status = TradeStatus.Filled,
                IsPaper = false,
                TxHash = txHash,
            };

            lock (_lock)
            {
                _openTrades.Add(filled);
                _bankroll -= filled.Outlay;
            }

            _log.Info(
                $"LIVE TRADE: {Truncate(filled.MarketQuestion)} | " +
                $"Price={filled.EntryPrice:F3} Shares={filled.Shares:F2} " +
                $"Outlay=${filled.Outlay:F2} TxHash={txHash} Bankroll=${_bankroll:F2}");

            return filled;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _log.Error($"Live trade execution failed for: {Truncate(trade.MarketQuestion)}", ex);
            throw;
        }
    }

    /// <summary>
    /// Generates a random salt for the EIP-712 order nonce.
    /// </summary>
    private static string GenerateRandomSalt()
    {
        byte[] bytes = new byte[32];
        Random.Shared.NextBytes(bytes);
        return new BigInteger(bytes, isUnsigned: true).ToString();
    }

    /// <summary>
    /// Builds the EIP-712 typed data JSON string for a Polymarket CLOB order.
    /// Follows the MetaMask/EIP-712 JSON specification format.
    /// </summary>
    private static string BuildEip712Json(
        string salt, string maker, string tokenId,
        string makerAmount, string takerAmount)
    {
        var typedData = new
        {
            types = new Dictionary<string, object[]>
            {
                ["EIP712Domain"] =
                [
                    new { name = "name", type = "string" },
                    new { name = "version", type = "string" },
                    new { name = "chainId", type = "uint256" },
                    new { name = "verifyingContract", type = "address" },
                ],
                ["Order"] =
                [
                    new { name = "salt", type = "uint256" },
                    new { name = "maker", type = "address" },
                    new { name = "signer", type = "address" },
                    new { name = "taker", type = "address" },
                    new { name = "tokenId", type = "uint256" },
                    new { name = "makerAmount", type = "uint256" },
                    new { name = "takerAmount", type = "uint256" },
                    new { name = "expiration", type = "uint256" },
                    new { name = "nonce", type = "uint256" },
                    new { name = "feeRateBps", type = "uint256" },
                    new { name = "side", type = "uint8" },
                    new { name = "signatureType", type = "uint8" },
                ],
            },
            primaryType = "Order",
            domain = new
            {
                name = Eip712DomainName,
                version = Eip712DomainVersion,
                chainId = PolygonChainId.ToString(),
                verifyingContract = ExchangeContractAddress,
            },
            message = new
            {
                salt,
                maker,
                signer = maker,
                taker = "0x0000000000000000000000000000000000000000",
                tokenId,
                makerAmount,
                takerAmount,
                expiration = "0",
                nonce = "0",
                feeRateBps = "0",
                side = "0",          // BUY = 0
                signatureType = "2", // EOA
            },
        };

        return JsonSerializer.Serialize(typedData);
    }

    /// <summary>
    /// Converts a decimal amount to on-chain base units (e.g. 6 decimals for USDC).
    /// </summary>
    /// <param name="amount">Human-readable amount (e.g. 100.50).</param>
    /// <param name="decimals">Number of decimal places for the token.</param>
    /// <returns>Amount in smallest token units as a <see cref="BigInteger"/>.</returns>
    private static BigInteger ToBaseUnits(double amount, int decimals)
    {
        decimal scaled = (decimal)amount * (decimal)Math.Pow(10, decimals);
        return new BigInteger(Math.Floor(scaled));
    }

    /// <summary>
    /// Attempts to extract an order ID or transaction hash from the CLOB API response JSON.
    /// </summary>
    private static string? ExtractOrderId(string responseBody)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("orderID", out JsonElement orderId))
                return orderId.GetString();
            if (root.TryGetProperty("orderId", out JsonElement orderId2))
                return orderId2.GetString();
            if (root.TryGetProperty("transactionHash", out JsonElement txHash))
                return txHash.GetString();
            if (root.TryGetProperty("hash", out JsonElement hash))
                return hash.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Truncates a string to a maximum length for log readability.
    /// </summary>
    private static string Truncate(string text, int max = 80) =>
        text.Length <= max ? text : string.Concat(text.AsSpan(0, max - 1), "…");
}
