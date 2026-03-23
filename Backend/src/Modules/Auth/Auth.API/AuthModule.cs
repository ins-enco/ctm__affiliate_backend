using Auth.Application.Services;
using Auth.Application.Settings;

namespace Auth.API;

public class AuthModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings is not configured.");
        services.AddSingleton(jwtSettings);

        services.AddScoped<IAuthService, AuthService>();
    }

    public void MapEndpoints(IApplicationBuilder app) { }
}
