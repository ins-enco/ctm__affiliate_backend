using CopyTradeMarketApi.Shared.Mail;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Templates;

public class TemplateResolver(IEnumerable<IEmailTemplateProvider> providers, ILogger<TemplateResolver> logger) : ITemplateResolver
{
    public async Task<EmailTemplate> ResolveAsync(string name)
    {
        foreach (var provider in providers)
        {
            var template = await provider.GetTemplateAsync(name);
            if (template is not null)
            {
                logger.LogDebug("Template '{Name}' resolved by {Provider}", name, provider.GetType().Name);
                return template;
            }
        }

        throw new InvalidOperationException($"Email template '{name}' could not be found in any configured datasource.");
    }
}
