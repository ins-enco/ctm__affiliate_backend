using System.ComponentModel.DataAnnotations;
using CopyTradeMarketApi.Shared.Validation;

namespace Auth.Application.Tests.Validation;

public class StrictEmailFieldAttributeTests
{
    private static ValidationResult? Validate(string? value)
    {
        var attr = new StrictEmailFieldAttribute();
        var ctx  = new ValidationContext(new object()) { DisplayName = "Email" };
        return attr.GetValidationResult(value, ctx);
    }

    // ── Valid formats — must pass ──────────────────────────────────────────────

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user@mail.example.co.uk")]   // subdomain + multi-part TLD
    [InlineData("user+tag@example.org")]       // plus-addressing
    [InlineData("u@x.io")]                     // short but valid
    [InlineData("USER@EXAMPLE.COM")]           // uppercase (case-insensitive)
    [InlineData("  user@example.com  ")]       // leading/trailing whitespace trimmed
    public void Validate_WithValidEmail_ReturnsSuccess(string email)
    {
        Assert.Equal(ValidationResult.Success, Validate(email));
    }

    // ── Invalid formats — must fail ───────────────────────────────────────────

    [Theory]
    [InlineData("user@e")]            // single-char domain, no TLD
    [InlineData("user@example")]      // missing TLD
    [InlineData("userexample.com")]   // missing @
    [InlineData("@example.com")]      // missing local part
    [InlineData("user@@example.com")] // double @
    [InlineData("user @example.com")] // space inside
    public void Validate_WithInvalidEmail_ReturnsError(string email)
    {
        var result = Validate(email);
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("valid email", result!.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ── Null / empty — defers to [Required], must not double-error ────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmpty_ReturnsSuccess_DefersToRequired(string? value)
    {
        // StrictEmailField must not produce its own error for missing values.
        // [Required] handles that; stacking both would produce duplicate messages.
        Assert.Equal(ValidationResult.Success, Validate(value));
    }
}
