using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Ether_Lite.Models;
using Ether_Lite.Services.Interface;

namespace Ether_Lite.Services
{
    public sealed class WalletInfoService : IWalletInfoService,IWalletBalService
    {
        private readonly ILogger<WalletInfoService> _logger;
        private readonly Dictionary<string, IWeb3> _web3Clients;

        public WalletInfoService(IConfiguration configuration,
                                 ILogger<WalletInfoService> logger)
        {
            _logger = logger;
            _web3Clients = new(StringComparer.OrdinalIgnoreCase);

            // Read every network under the "Ethereum" section
            IConfigurationSection ethSection = configuration.GetSection("Ethereum");
            string alchemyApi = configuration["AlchemyApi"];

            if (!ethSection.Exists())
            {
                _logger.LogCritical("Configuration section 'Ethereum' not found.");
                throw new InvalidOperationException("Missing 'Ethereum' section in configuration.");
            }

            foreach (IConfigurationSection child in ethSection.GetChildren())
            {
                string networkName = child.Key;           // "Eth", "Sep", etc.
                string? baseUrl = child.Value;            // base URL without key

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    _logger.LogWarning("RPC base URL missing for network '{Network}'. Skipping.", networkName);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(alchemyApi))
                {
                    _logger.LogCritical("Alchemy API key is missing. Cannot construct full RPC URLs.");
                    throw new InvalidOperationException("Missing Alchemy API key in configuration.");
                }

                try
                {
                    string fullUrl = baseUrl.EndsWith("/")
                        ? baseUrl + alchemyApi
                        : baseUrl + "/" + alchemyApi;

                    _web3Clients[networkName] = new Web3(fullUrl);
                    _logger.LogInformation("Initialized Web3 client for {Network}.", networkName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Web3 client for {Network}.", networkName);
                }
            }

            if (_web3Clients.Count == 0)
            {
                _logger.LogCritical("No Web3 clients were initialized successfully.");
                throw new InvalidOperationException("No blockchain networks could be initialized.");
            }


            if (_web3Clients.Count == 0)
            {
                _logger.LogCritical("No Web3 clients were initialized successfully.");
                throw new InvalidOperationException("No blockchain networks could be initialized.");
            }
        }

        /// <summary>Returns a Web3 client for the requested network.</summary>
        public IWeb3 GetClient(string networkName)
        {
            if (_web3Clients.TryGetValue(networkName, out var web3))
                return web3;

            // Try case-insensitive match
            var match = _web3Clients
                .FirstOrDefault(x => string.Equals(x.Key, networkName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(match.Key))
                return match.Value;

            throw new KeyNotFoundException($"Network '{networkName}' is not configured.");
        }
        public async Task<List<WalletBalance>> GetTopWalletsByBalance(string network, int topN = 10)
        {
            if (!_web3Clients.TryGetValue(network, out var web3))
                throw new ArgumentException($"Unsupported network: {network}", nameof(network));

            var result = new List<WalletBalance>();
            var latestBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(latestBlock));

            // Get unique addresses from recent transactions
            var uniqueAddresses = block.Transactions
                .SelectMany(t => new[] { t.From, t.To })
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(1000) // Limit to 1000 addresses for performance
                .ToList();

            // Get balances for each address
            var balanceTasks = uniqueAddresses.Select(async address =>
            {
                var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(address);
                return new WalletBalance
                {
                    Address = address,
                    BalanceInEth = Web3.Convert.FromWei(balanceWei)
                };
            });

            var balances = await Task.WhenAll(balanceTasks);

            return balances
                .OrderByDescending(w => w.BalanceInEth)
                .Take(topN)
                .ToList();
        }

        public async Task<WalletInfoResult> GetWalletInfo(
            string network,
            string address,
            int limit = 1_000_000)
        {
            // 1️⃣  Validate input & get client
            if (!_web3Clients.TryGetValue(network, out var web3))
                throw new ArgumentException($"Unsupported network: {network}", nameof(network));

            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(address))
                throw new ArgumentException("Invalid Ethereum address.", nameof(address));

            // 2️⃣  Basic account info
            var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(address);
            var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(address);
            var balanceEth = Web3.Convert.FromWei(balanceWei);

            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            var gasPriceG = Web3.Convert.FromWei(gasPrice, UnitConversion.EthUnit.Gwei);

            if (txCount.Value == 0)
            {
                return new WalletInfoResult
                {
                    Message = "This wallet has no transactions yet.",
                    WalletAddress = address,
                    CurrentBalanceInEth = balanceEth,
                    CurrentGasPriceInGwei = gasPriceG,
                    Network = network
                };
            }

            // 3️⃣  Scan blocks backwards (up to `limit`)
            var latestBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            BigInteger startBlock = latestBlock.Value;
            BigInteger endBlock = BigInteger.Max(BigInteger.Zero, startBlock - limit);

            for (BigInteger i = startBlock; i >= endBlock; i--)
            {
                var block = await web3.Eth.Blocks
                    .GetBlockWithTransactionsByNumber
                    .SendRequestAsync(new BlockParameter(new HexBigInteger(i)));

                var relatedTxs = block.Transactions
                    .Where(t => string.Equals(t.From, address, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(t.To, address, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!relatedTxs.Any()) continue;

                var lastTx = relatedTxs.Last();
                var txBlock = await web3.Eth.Blocks
                    .GetBlockWithTransactionsByNumber
                    .SendRequestAsync(new BlockParameter(lastTx.BlockNumber));

                // Timestamp → UTC
                long unixSeconds = (long)txBlock.Timestamp.Value;
                var txDateTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;

                return new WalletInfoResult
                {
                    WalletAddress = address,
                    CurrentBalanceInEth = balanceEth,
                    CurrentGasPriceInGwei = gasPriceG,
                    ScannedBlock = (long)i,
                    Network = network,
                    LastTransaction = new TransactionInfo
                    {
                        TxHash = lastTx.TransactionHash,
                        From = lastTx.From,
                        To = lastTx.To,
                        ValueInEth = Web3.Convert.FromWei(lastTx.Value),
                        GasUsed = lastTx.Gas.Value,
                        BlockNumber = lastTx.BlockNumber.Value,
                        DateTimeUtc = txDateTime
                    }
                };
            }

            // 4️⃣  No tx in scanned range
            return new WalletInfoResult
            {
                Message = $"No transactions found in the last {limit} blocks.",
                WalletAddress = address,
                CurrentBalanceInEth = balanceEth,
                CurrentGasPriceInGwei = gasPriceG,
                Network = network
            };
        }
    }
}
