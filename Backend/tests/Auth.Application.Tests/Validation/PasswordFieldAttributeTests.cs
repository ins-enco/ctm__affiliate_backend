namespace Auth.Application.Tests.Validation;

public class PasswordFieldAttributeTests
{
    private static ValidationResult? Validate(string? value)
    {
        var attr = new PasswordFieldAttribute();
        var ctx  = new ValidationContext(new object()) { DisplayName = "Password" };
        return attr.GetValidationResult(value, ctx);
    }

    private static string[] Messages(ValidationResult? result) =>
        result?.ErrorMessage?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];

    [Fact]
    public void Validate_WithValidPassword_ReturnsSuccess()
    {
        var result = Validate("ValidPass1!");
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Validate_WithNullValue_ReturnsSuccess()
    {
        // null defers to [Required] — PasswordField should not produce its own error
        var result = Validate(null);
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Validate_WithEmptyString_ReturnsSuccess()
    {
        var result = Validate("");
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Validate_WithShortPassword_ReturnsLengthError()
    {
        var msgs = Messages(Validate("Ab1!"));
        Assert.Contains(msgs, m => m.Contains("at least 8 characters"));
    }

    [Fact]
    public void Validate_WithNoUppercase_ReturnsUppercaseError()
    {
        var msgs = Messages(Validate("validpass1!"));
        Assert.Contains(msgs, m => m.Contains("uppercase"));
    }

    [Fact]
    public void Validate_WithNoDigit_ReturnsDigitError()
    {
        var msgs = Messages(Validate("ValidPass!"));
        Assert.Contains(msgs, m => m.Contains("digit"));
    }

    [Fact]
    public void Validate_WithNoSpecialChar_ReturnsSpecialCharError()
    {
        var msgs = Messages(Validate("ValidPass1"));
        Assert.Contains(msgs, m => m.Contains("special character"));
    }

    [Fact]
    public void Validate_WithMultipleViolations_ReturnsAllErrors()
    {
        // "short" → fails length, uppercase, digit, special char
        var msgs = Messages(Validate("short"));
        Assert.True(msgs.Length >= 3, $"Expected ≥3 errors, got: {string.Join(", ", msgs)}");
    }
}
