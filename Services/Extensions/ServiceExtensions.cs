using Ether_Lite.Services.Interface;
using Ether_Lite.Services;

namespace Ether_Lite.Services.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddWalletServices(this IServiceCollection services)
        {
            services.AddScoped<IWalletInfoService, WalletInfoService>();
            return services;
        }
    }
}
