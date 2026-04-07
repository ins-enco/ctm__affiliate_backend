namespace Auth.Application.Tests.Validation;

public class PhoneCodeFieldAttributeTests
{
    private static ValidationResult? Validate(string? value)
    {
        var attr = new PhoneCodeFieldAttribute();
        var ctx  = new ValidationContext(new object()) { DisplayName = "PhoneCode" };
        return attr.GetValidationResult(value, ctx);
    }

    [Theory]
    [InlineData("+84")]
    [InlineData("+1")]
    [InlineData("+886")]
    [InlineData("+44")]
    [InlineData("+9999")]
    public void Validate_WithValidCode_ReturnsSuccess(string code)
    {
        Assert.Equal(ValidationResult.Success, Validate(code));
    }

    [Theory]
    [InlineData("84")]         // missing leading +
    [InlineData("+0")]         // zero after + not allowed
    [InlineData("+")]          // + only
    [InlineData("+12345")]     // too many digits (>4)
    [InlineData("++84")]       // double +
    [InlineData("abc")]        // non-numeric
    public void Validate_WithInvalidCode_ReturnsError(string code)
    {
        var result = Validate(code);
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("dial code", result!.ErrorMessage);
    }

    [Fact]
    public void Validate_WithNull_ReturnsSuccess() =>
        Assert.Equal(ValidationResult.Success, Validate(null));

    [Fact]
    public void Validate_WithEmptyString_ReturnsSuccess() =>
        Assert.Equal(ValidationResult.Success, Validate(""));
}

public class PhoneNumberFieldAttributeTests
{
    private static ValidationResult? Validate(string? value)
    {
        var attr = new PhoneNumberFieldAttribute();
        var ctx  = new ValidationContext(new object()) { DisplayName = "PhoneNumber" };
        return attr.GetValidationResult(value, ctx);
    }

    [Theory]
    [InlineData("901234567")]
    [InlineData("2025551234")]
    [InlineData("12345")]        // minimum 5 digits
    [InlineData("123456789012345")] // maximum 15 digits
    public void Validate_WithValidNumber_ReturnsSuccess(string number)
    {
        Assert.Equal(ValidationResult.Success, Validate(number));
    }

    [Theory]
    [InlineData("1234")]           // too short (<5)
    [InlineData("1234567890123456")] // too long (>15)
    [InlineData("+84901234567")]   // contains country code — not allowed
    [InlineData("090-1234-567")]   // contains dashes
    [InlineData("abc12345")]       // non-numeric
    public void Validate_WithInvalidNumber_ReturnsError(string number)
    {
        var result = Validate(number);
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("local phone number", result!.ErrorMessage);
    }

    [Fact]
    public void Validate_WithNull_ReturnsSuccess() =>
        Assert.Equal(ValidationResult.Success, Validate(null));

    [Fact]
    public void Validate_WithEmptyString_ReturnsSuccess() =>
        Assert.Equal(ValidationResult.Success, Validate(""));
}
