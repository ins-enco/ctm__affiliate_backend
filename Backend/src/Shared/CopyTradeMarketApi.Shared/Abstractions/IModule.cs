namespace CopyTradeMarketApi.Shared.Abstractions;

public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
    void MapEndpoints(IApplicationBuilder app);
}
