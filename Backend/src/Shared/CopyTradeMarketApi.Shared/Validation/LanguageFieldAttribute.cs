namespace CopyTradeMarketApi.Shared.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class LanguageFieldAttribute : ValidationAttribute
{
    private static readonly Regex LanguageRegex =
        new(@"^[a-z]{2}(-[A-Z]{2})?$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string lang || string.IsNullOrEmpty(lang))
            return ValidationResult.Success; // defer to [Required]

        return LanguageRegex.IsMatch(lang)
            ? ValidationResult.Success
            : new ValidationResult("Language must be a valid BCP 47 language code (e.g. 'en', 'vi', 'en-US').");
    }
}
