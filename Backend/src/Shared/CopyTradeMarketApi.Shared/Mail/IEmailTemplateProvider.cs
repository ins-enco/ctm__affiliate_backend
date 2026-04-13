namespace CopyTradeMarketApi.Shared.Mail;

/// <summary>
/// Loads email templates from a specific datasource.
/// Returns <c>null</c> when the named template is not available in this source,
/// allowing the caller to fall through to the next provider.
/// </summary>
public interface IEmailTemplateProvider
{
    Task<EmailTemplate?> GetTemplateAsync(string name);
}
