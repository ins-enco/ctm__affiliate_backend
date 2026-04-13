using CopyTradeMarketApi.Shared.Mail;

namespace Auth.Application.Templates;

/// <summary>
/// Resolves an email template by name by iterating registered <see cref="IEmailTemplateProvider"/> sources in order.
/// Throws <see cref="InvalidOperationException"/> if no provider can supply the template.
/// </summary>
public interface ITemplateResolver
{
    Task<EmailTemplate> ResolveAsync(string name);
}
