using System.Net;
using System.Net.Mail;
using CopyTradeMarketApi.Shared.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Mail;

public class SmtpMailService(IConfiguration configuration, ILogger<SmtpMailService> logger) : IMailService
{
    public async Task SendAsync(CopyTradeMarketApi.Shared.Mail.MailMessage message)
    {
        var host       = configuration["MailSettings:SmtpHost"]     ?? string.Empty;
        var port       = configuration.GetValue<int>("MailSettings:SmtpPort", 587);
        var username   = configuration["MailSettings:SmtpUsername"] ?? string.Empty;
        var password   = configuration["MailSettings:SmtpPassword"] ?? string.Empty;
        var fromAddr   = configuration["MailSettings:FromAddress"]  ?? string.Empty;
        var fromName   = configuration["MailSettings:FromName"]     ?? "CopyTradeMarket";
        var useSsl     = configuration.GetValue<bool>("MailSettings:UseSsl", true);

        logger.LogDebug("Dispatching email to {Recipient} with subject '{Subject}'", message.To, message.Subject);
        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl   = useSsl,
                Credentials = new NetworkCredential(username, password)
            };

            using var mail = new System.Net.Mail.MailMessage
            {
                From       = new MailAddress(fromAddr, fromName),
                Subject    = message.Subject,
                Body       = message.Body,
                IsBodyHtml = true
            };
            mail.To.Add(message.To);

            await client.SendMailAsync(mail);
            logger.LogInformation("Email dispatched successfully to {Recipient}", message.To);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch email to {Recipient}", message.To);
            throw;
        }
    }
}
