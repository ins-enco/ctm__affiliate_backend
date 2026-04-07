namespace Auth.Application.Templates;

public record VerificationEmailContext(string RecipientName, string VerificationLink, string ExpiryDescription)
{
    public string Render(string template)
        => template
            .Replace("{{RecipientName}}", RecipientName)
            .Replace("{{VerificationLink}}", VerificationLink)
            .Replace("{{ExpiryDescription}}", ExpiryDescription);
}
