namespace Mock.API;

/// <summary>Registers the Mock module's services. Active in Development environment only.</summary>
public class MockModule : IModule
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMockService, MockService>();
    }

    /// <inheritdoc />
    public void MapEndpoints(IApplicationBuilder app) { }
}
