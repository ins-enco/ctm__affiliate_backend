using CopyTradeMarketApi.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.API;

public class AuthModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Auth services registered here in later parts
    }

    public void MapEndpoints(IApplicationBuilder app)
    {
        // Auth endpoints mapped here in later parts
    }
}
