namespace SubscriptionHistory.API;

/// <summary>Registers the SubscriptionHistory module's services and endpoints.</summary>
public class SubscriptionHistoryModule : IModule
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISubscriptionHistoryService, SubscriptionHistoryService>();
    }

    /// <inheritdoc />
    public void MapEndpoints(IApplicationBuilder app) { }
}
