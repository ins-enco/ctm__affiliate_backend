using Auth.Application.EventHandlers;
using Auth.Application.Services;
using Auth.Application.Settings;
using Auth.Application.Templates;
using Auth.Infrastructure.Mail;
using CopyTradeMarketApi.Shared.Events;
using CopyTradeMarketApi.Shared.Mail;
using CopyTradeMarketApi.Shared.Verification;

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

        // Verification settings — reads TokenExpiryHours from config; swap implementation for DB-backed settings in future
        services.AddSingleton<IVerificationSettings, AppSettingsVerificationSettings>();

        // Mail service
        services.AddScoped<IMailService, SmtpMailService>();

        // Template providers (order matters — FileSystem is checked first, then Database)
        services.AddScoped<IEmailTemplateProvider, FileSystemTemplateProvider>();

        // Template resolver — iterates registered providers in DI order
        services.AddScoped<ITemplateResolver, TemplateResolver>();

        // Verification service
        services.AddScoped<IVerificationService, VerificationService>();

        // Auth service (depends on IVerificationService)
        services.AddScoped<IAuthService, AuthService>();

        // Event handlers
        services.AddScoped<IEventHandler<UserRegisteredEvent>, EmailVerificationEventHandler>();
    }

    public void MapEndpoints(IApplicationBuilder app) { }
}
