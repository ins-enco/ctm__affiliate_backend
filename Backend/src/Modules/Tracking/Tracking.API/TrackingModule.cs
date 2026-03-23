using CopyTradeMarketApi.Shared.Abstractions;
using CopyTradeMarketApi.Shared.Events;
using Tracking.Application.EventHandlers;
using Tracking.Application.Services;

namespace Tracking.API;

public class TrackingModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<TrackingDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

        services.AddScoped<ITrackingService, TrackingService>();
        services.AddScoped<IClickStatsReader, ClickStatsReader>();
        services.AddScoped<IEventHandler<UserRegisteredEvent>, UserRegisteredEventHandler>();
    }

    public void MapEndpoints(IApplicationBuilder app) { }
}
