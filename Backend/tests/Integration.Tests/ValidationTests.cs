namespace Integration.Tests;

public class ValidationTests : IClassFixture<IntegrationWebFactory>
{
    private readonly HttpClient _client;

    public ValidationTests(IntegrationWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static async Task<Dictionary<string, string[]>> ReadErrors(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // ProblemDetails.Extensions are serialized as flat top-level properties
        var errorsElement = body.GetProperty("errors");
        return JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsElement.GetRawText())!;
    }

    // ── Register — required fields ─────────────────────────────────────────────

    [Fact]
    public async Task Register_WithEmptyBody_Returns403WithAllFieldErrors()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new { });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        var errors = await ReadErrors(resp);
        Assert.True(errors.ContainsKey("name")     || errors.ContainsKey("Name"),     "Missing 'name' error");
        Assert.True(errors.ContainsKey("email")    || errors.ContainsKey("Email"),    "Missing 'email' error");
        Assert.True(errors.ContainsKey("password") || errors.ContainsKey("Password"), "Missing 'password' error");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@e")]          // single-char domain, no TLD
    [InlineData("user@example")]    // missing TLD
    [InlineData("@example.com")]    // missing local part
    [InlineData("userexample.com")] // missing @
    public async Task Register_WithInvalidEmail_Returns403WithEmailError(string badEmail)
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name     = "Test User",
            email    = badEmail,
            password = "ValidPass1!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        var errors = await ReadErrors(resp);
        Assert.True(errors.ContainsKey("email") || errors.ContainsKey("Email"),
            $"Expected email error for input '{badEmail}'");
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns403WithPasswordSubRules()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name     = "Test User",
            email    = "valid@test.com",
            password = "weak"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        var errors = await ReadErrors(resp);
        var key = errors.ContainsKey("password") ? "password" : "Password";
        Assert.True(errors[key].Length >= 2, "Expected multiple password sub-rule errors");
    }

    // ── Login — required fields ────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithMissingFields_Returns403()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Register — happy path regression ──────────────────────────────────────

    [Fact]
    public async Task Register_WithValidPayload_Returns201()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name     = "Validation User",
            email    = "validation.user@test.com",
            password = "ValidPass1!"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }
}
