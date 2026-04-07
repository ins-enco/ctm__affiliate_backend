namespace CopyTradeMarketApi.Shared.Mail;

/// <summary>A named template with subject and body patterns containing {{placeholder}} markers.</summary>
public record EmailTemplate(string Name, string Subject, string Body);
