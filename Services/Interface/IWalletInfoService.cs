using Ether_Lite.Models;

namespace Ether_Lite.Services.Interface
{
    public interface IWalletInfoService
    {
        Task<WalletInfoResult> GetWalletInfo(string network, string address, int limit = 1000000);
    }
}
