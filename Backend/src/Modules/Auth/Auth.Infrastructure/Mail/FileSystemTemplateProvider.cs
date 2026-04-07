using CopyTradeMarketApi.Shared.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Mail;

public class FileSystemTemplateProvider(IConfiguration configuration, ILogger<FileSystemTemplateProvider> logger) : IEmailTemplateProvider
{
    private readonly string _basePath = configuration["EmailTemplates:FileSystemPath"] ?? "templates/email";

    public async Task<EmailTemplate?> GetTemplateAsync(string name)
    {
        var subjectFile = Path.Combine(_basePath, $"{name}.subject.txt");
        var bodyFile    = Path.Combine(_basePath, $"{name}.body.html");

        if (!File.Exists(subjectFile) || !File.Exists(bodyFile))
        {
            logger.LogDebug("Template '{Name}' not found in file system at '{Path}'", name, _basePath);
            return null;
        }

        var subject = await File.ReadAllTextAsync(subjectFile);
        var body    = await File.ReadAllTextAsync(bodyFile);

        logger.LogDebug("Template '{Name}' loaded from file system", name);
        return new EmailTemplate(name, subject.Trim(), body);
    }
}
