namespace Auth.Application.Tests.Validation;

public class LanguageFieldAttributeTests
{
    private static ValidationResult? Validate(string? value)
    {
        var attr = new LanguageFieldAttribute();
        var ctx  = new ValidationContext(new object()) { DisplayName = "Language" };
        return attr.GetValidationResult(value, ctx);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("vi")]
    [InlineData("zh")]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    public void Validate_WithValidLanguage_ReturnsSuccess(string lang)
    {
        Assert.Equal(ValidationResult.Success, Validate(lang));
    }

    [Theory]
    [InlineData("EN")]       // uppercase not allowed for base
    [InlineData("english")]  // too long
    [InlineData("e")]        // too short
    [InlineData("en-us")]    // region must be uppercase
    [InlineData("en_US")]    // wrong separator
    public void Validate_WithInvalidLanguage_ReturnsError(string lang)
    {
        var result = Validate(lang);
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("BCP 47", result!.ErrorMessage);
    }

    [Fact]
    public void Validate_WithNull_ReturnsSuccess()
    {
        Assert.Equal(ValidationResult.Success, Validate(null));
    }

    [Fact]
    public void Validate_WithEmptyString_ReturnsSuccess()
    {
        Assert.Equal(ValidationResult.Success, Validate(""));
    }
}
