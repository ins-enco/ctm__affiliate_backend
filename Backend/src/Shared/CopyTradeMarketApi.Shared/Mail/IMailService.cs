namespace CopyTradeMarketApi.Shared.Mail;

/// <summary>Sends a prepared mail message via the configured transport.</summary>
public interface IMailService
{
    Task SendAsync(MailMessage message);
}
