using Auth.Application.Services;
using Auth.Application.Templates;
using CopyTradeMarketApi.Shared.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Auth.Application.EventHandlers;

public class EmailVerificationEventHandler(
    AuthDbContext db,
    IVerificationService verificationService,
    ITemplateResolver templateResolver,
    IMailService mailService,
    IConfiguration configuration,
    ILogger<EmailVerificationEventHandler> logger) : IEventHandler<UserRegisteredEvent>
{
    private const string TemplateName = "email-verification";

    public async Task HandleAsync(UserRegisteredEvent domainEvent)
    {
        try
        {
            var user = await db.Users
                .Apply(new UserByIdSpecification(domainEvent.UserId, includeInformation: true))
                .FirstOrDefaultAsync();

            if (user is null)
            {
                logger.LogWarning("EmailVerificationEventHandler: user {UserId} not found, skipping dispatch", domainEvent.UserId);
                return;
            }

            var token    = await verificationService.CreateTokenAsync(user.Id, user.Email);
            var baseUrl  = configuration["BaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var link     = $"{baseUrl}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";
            var expiry   = configuration.GetValue<int>("EmailVerification:TokenExpiryHours", 24);
            var fullName = user.Information is not null
                ? $"{user.Information.FirstName} {user.Information.LastName}".Trim()
                : user.Email;

            var template = await templateResolver.ResolveAsync(TemplateName);
            var ctx      = new VerificationEmailContext(fullName, link, $"{expiry} hours");

            var message = new MailMessage(
                To:      user.Email,
                Subject: ctx.Render(template.Subject),
                Body:    ctx.Render(template.Body));

            await mailService.SendAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch verification email for user {UserId} — registration is unaffected", domainEvent.UserId);
        }
    }
}
