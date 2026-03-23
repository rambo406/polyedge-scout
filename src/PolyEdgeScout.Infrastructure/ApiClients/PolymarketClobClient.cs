namespace PolyEdgeScout.Infrastructure.ApiClients;

using System.Numerics;
using System.Text;
using System.Text.Json;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using PolyEdgeScout.Application.Configuration;
using PolyEdgeScout.Application.Interfaces;
using PolyEdgeScout.Domain.Entities;
using PolyEdgeScout.Domain.Interfaces;

/// <summary>
/// Signs and submits limit orders to the Polymarket CLOB using EIP-712 typed-data signing.
/// </summary>
public sealed class PolymarketClobClient : IClobClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>EIP-712 domain name for Polymarket CLOB orders.</summary>
    private const string Eip712DomainName = "Polymarket CTF Exchange";

    /// <summary>EIP-712 domain version for Polymarket CLOB orders.</summary>
    private const string Eip712DomainVersion = "1";

    /// <summary>Polygon chain ID.</summary>
    private const int PolygonChainId = 137;

    /// <summary>Polymarket CTF Exchange contract address on Polygon.</summary>
    private const string ExchangeAddress = "0x4bFb41d5B3570DeFd03C39a9A4D8dE6Bd8B8982E";

    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly ILogService _log;

    public PolymarketClobClient(HttpClient httpClient, AppConfig config, ILogService log)
    {
        _httpClient = httpClient;
        _config = config;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<string?> PostOrderAsync(Trade trade, CancellationToken ct = default)
    {
        string? privateKey = Environment.GetEnvironmentVariable("PRIVATE_KEY");
        if (string.IsNullOrWhiteSpace(privateKey))
        {
            _log.Error("PRIVATE_KEY environment variable is not set. Cannot submit CLOB order.");
            return null;
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
            string tokenIdValue = trade.TokenId.Length > 0 ? trade.TokenId : "0";

            // Build EIP-712 typed data JSON
            string eip712Json = BuildEip712Json(
                salt, walletAddress, tokenIdValue,
                sharesBaseUnits.ToString(), costBaseUnits.ToString());

            // Sign the EIP-712 typed data using Nethereum
            Eip712TypedDataSigner signer = new();
            string signature = signer.SignTypedDataV4(eip712Json, ecKey);

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

            using HttpResponseMessage response = await _httpClient.PostAsync(clobUrl, content, ct);
            string responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _log.Error($"CLOB order failed ({response.StatusCode}): {responseBody}");
                return null;
            }

            string? orderId = ExtractOrderId(responseBody);
            _log.Info($"Order accepted — ID: {orderId ?? "unknown"}");
            return orderId;
        }
        catch (OperationCanceledException)
        {
            throw; // propagate cancellation
        }
        catch (Exception ex)
        {
            _log.Error($"CLOB order submission failed for: {trade.MarketQuestion}", ex);
            return null;
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
                verifyingContract = ExchangeAddress,
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
}
