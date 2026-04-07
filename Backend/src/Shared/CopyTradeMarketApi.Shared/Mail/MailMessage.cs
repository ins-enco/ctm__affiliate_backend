namespace CopyTradeMarketApi.Shared.Mail;

/// <summary>A fully-rendered, ready-to-send email message.</summary>
public record MailMessage(string To, string Subject, string Body);
