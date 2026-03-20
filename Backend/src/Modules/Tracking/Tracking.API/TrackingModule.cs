using CopyTradeMarketApi.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tracking.API;

public class TrackingModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Tracking services registered here in later parts
    }

    public void MapEndpoints(IApplicationBuilder app)
    {
        // Tracking endpoints mapped here in later parts
    }
}
