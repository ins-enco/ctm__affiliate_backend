using CopyTradeMarketApi.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Affiliate.API;

public class AffiliateModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Affiliate services registered here in later parts
    }

    public void MapEndpoints(IApplicationBuilder app)
    {
        // Affiliate endpoints mapped here in later parts
    }
}
