using Ether_Lite.Models;

namespace Ether_Lite.Services.Interface
{
    public interface IWalletBalService
    {
        // Existing methods...
        Task<List<WalletBalance>> GetTopWalletsByBalance(string network, int topN = 10);
    }

    
}
