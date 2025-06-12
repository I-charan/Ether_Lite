using Microsoft.AspNetCore.Mvc;
using Ether_Lite.Services.Interface;

namespace Ether_Lite.Controllers
{
    [ApiController]
    [Route("api/wallet-info")]
    public class WalletInfoController : ControllerBase
    {
        private readonly IWalletInfoService _walletInfoService;
        private readonly IWalletBalService _walletBalService;
        private readonly ILogger<WalletInfoController> _logger;

        public WalletInfoController(
            IWalletInfoService walletInfoService,
            ILogger<WalletInfoController> logger,IWalletBalService walletBalService)
        {
            _walletInfoService = walletInfoService;
            _logger = logger;
            _walletBalService = walletBalService;
        }

        // ------------------------
        // ETH Mainnet & Sepolia
        // ------------------------
        [HttpGet("ethereum/mainnet/{address}")]
        public Task<IActionResult> GetMainnetWalletInfo(string address, [FromQuery] int limit = 10000000)
            => GetWalletInfo("Eth", address, limit);

        [HttpGet("ethereum/sepolia/{address}")]
        public Task<IActionResult> GetSepoliaWalletInfo(string address, [FromQuery] int limit = 10000000)
            => GetWalletInfo("Sep", address, limit);

        // ------------------------
        // Arbitrum, Polygon, Optimism
        // ------------------------
        [HttpGet("arb/{address}")]
        public Task<IActionResult> GetArbWalletInfo(string address, [FromQuery] int limit = 10000000)
            => GetWalletInfo("Arb", address, limit);

        [HttpGet("polygon/mainnet/{address}")]
        public Task<IActionResult> GetPolygonMainnetWalletInfo(string address, [FromQuery] int limit = 10000000)
            => GetWalletInfo("Pol", address, limit);

        [HttpGet("op/{address}")]
        public Task<IActionResult> GetOpWalletInfo(string address, [FromQuery] int limit = 10000000)
            => GetWalletInfo("Op", address, limit);

        [HttpGet("top-balances/{network}")]
        public async Task<IActionResult> GetTopBalances(string network, [FromQuery] int top = 10)
        {
            try
            {
                var topWallets = await _walletBalService.GetTopWalletsByBalance(network, top);
                return Ok(topWallets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top balances");
                return BadRequest(ex.Message);
            }
        }
        // =====================================================================
        //  Centralised helper — returns *one* Ok(dto) so no extra wrapper JSON
        // =====================================================================
        private async Task<IActionResult> GetWalletInfo(string network, string address, int limit)
        {
            try
            {
                var dto = await _walletInfoService.GetWalletInfo(network, address, limit);

                if (dto is null)
                {
                    _logger.LogWarning("No data returned for {Address} on {Network}", address, network);
                    return NotFound("Wallet information not available");
                }

                return Ok(dto);               // <-- SINGLE Ok()   ✔  no extra wrapper
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for {Address}", address);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for {Address}", address);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}
