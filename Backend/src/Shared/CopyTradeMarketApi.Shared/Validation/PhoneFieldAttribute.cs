namespace CopyTradeMarketApi.Shared.Validation;

/// <summary>
/// Validates a country dial code, e.g. "+84", "+1", "+886".
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PhoneCodeFieldAttribute : ValidationAttribute
{
    private static readonly Regex CodeRegex =
        new(@"^\+[1-9]\d{0,3}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string code || string.IsNullOrEmpty(code))
            return ValidationResult.Success; // defer to [Required]

        return CodeRegex.IsMatch(code)
            ? ValidationResult.Success
            : new ValidationResult("PhoneCode must be a valid country dial code (e.g. '+84', '+1').");
    }
}

/// <summary>
/// Validates a local subscriber phone number (digits only, no country code), e.g. "901234567".
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PhoneNumberFieldAttribute : ValidationAttribute
{
    private static readonly Regex NumberRegex =
        new(@"^\d{5,15}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string number || string.IsNullOrEmpty(number))
            return ValidationResult.Success; // defer to [Required]

        return NumberRegex.IsMatch(number)
            ? ValidationResult.Success
            : new ValidationResult("PhoneNumber must be a valid local phone number (digits only, no country code).");
    }
}
