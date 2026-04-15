namespace Mock.API;

/// <summary>Registers the Mock module's services. Active in Development environment only.</summary>
public class MockModule : IModule
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMockService, MockService>();
        services.AddScoped<DevApiKeyFilter>();
    }

    /// <inheritdoc />
    public void MapEndpoints(IApplicationBuilder app) { }
}
