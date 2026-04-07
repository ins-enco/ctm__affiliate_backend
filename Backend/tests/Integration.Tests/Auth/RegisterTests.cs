namespace Integration.Tests.Auth;

/// <summary>
/// Integration tests for POST /api/auth/register with the v2 payload shape.
/// Covers validation enforcement (P2) and regression of existing affiliate/auth flows (P3).
/// </summary>
public class RegisterTests : IClassFixture<IntegrationWebFactory>
{
    private readonly IntegrationWebFactory _factory;

    public RegisterTests(IntegrationWebFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static object ValidPayload(string email, string firstName = "Test", string lastName = "User") => new
    {
        userInformation = new
        {
            firstName   = firstName,
            lastName    = lastName,
            email       = email,
            phoneCode   = "+84",
            phoneNumber = "901234567",
            language    = "vi"
        },
        password        = "Secure@123",
        confirmPassword = "Secure@123"
    };

    private static async Task<Dictionary<string, string[]>> ReadErrors(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errorsElement = body.GetProperty("errors");
        return JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsElement.GetRawText())!;
    }

    // ── T013 (US2): Happy path — returns 201 with userId and email ─────────────

    [Fact]
    public async Task Register_WithValidPayload_Returns201WithUserIdAndEmail()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            ValidPayload("register.valid@test.com", "Reg", "Valid"));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var result = await resp.Content.ReadFromJsonAsync<RegisterResult>();
        Assert.NotNull(result);
        Assert.True(result!.UserId > 0);
        Assert.Equal("register.valid@test.com", result.Email);
    }

    // ── T014 (US2): Invalid phone number ──────────────────────────────────────

    [Fact]
    public async Task Register_WithInvalidPhoneNumber_Returns400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            userInformation = new
            {
                firstName   = "Test",
                lastName    = "User",
                email       = "bad.phone@test.com",
                phoneCode   = "+84",
                phoneNumber = "123",     // too short — fails PhoneNumberField
                language    = "vi"
            },
            password        = "Secure@123",
            confirmPassword = "Secure@123"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        var errors = await ReadErrors(resp);
        var hasPhoneNumberError = errors.ContainsKey("UserInformation.PhoneNumber")
                               || errors.ContainsKey("userInformation.phoneNumber");
        Assert.True(hasPhoneNumberError, "Expected validation error for UserInformation.PhoneNumber");
    }

    // ── T015 (US2): Invalid language ──────────────────────────────────────────

    [Fact]
    public async Task Register_WithInvalidLanguage_Returns400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            userInformation = new
            {
                firstName   = "Test",
                lastName    = "User",
                email       = "bad.lang@test.com",
                phoneCode   = "+84",
                phoneNumber = "901234567",
                language    = "english"  // fails LanguageField
            },
            password        = "Secure@123",
            confirmPassword = "Secure@123"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        var errors = await ReadErrors(resp);
        var hasLanguageError = errors.ContainsKey("UserInformation.Language")
                            || errors.ContainsKey("userInformation.language");
        Assert.True(hasLanguageError, "Expected validation error for UserInformation.Language");
    }

    // ── T016 (US2): Mismatched passwords ──────────────────────────────────────

    [Fact]
    public async Task Register_WithMismatchedPasswords_Returns400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            userInformation = new
            {
                firstName   = "Test",
                lastName    = "User",
                email       = "mismatch.pass@test.com",
                phoneCode   = "+84",
                phoneNumber = "901234567",
                language    = "vi"
            },
            password        = "Secure@123",
            confirmPassword = "Different@123"   // mismatch
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        var errors = await ReadErrors(resp);
        var hasConfirmError = errors.ContainsKey("ConfirmPassword")
                           || errors.ContainsKey("confirmPassword");
        Assert.True(hasConfirmError, "Expected validation error for ConfirmPassword");
    }

    // ── T017 (US2): Missing first name ────────────────────────────────────────

    [Fact]
    public async Task Register_WithMissingFirstName_Returns400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            userInformation = new
            {
                firstName   = (string?)null,    // missing
                lastName    = "User",
                email       = "no.firstname@test.com",
                phoneCode   = "+84",
                phoneNumber = "901234567",
                language    = "vi"
            },
            password        = "Secure@123",
            confirmPassword = "Secure@123"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        var errors = await ReadErrors(resp);
        var hasFirstNameError = errors.ContainsKey("UserInformation.FirstName")
                             || errors.ContainsKey("userInformation.firstName");
        Assert.True(hasFirstNameError, "Expected validation error for UserInformation.FirstName");
    }

    // ── T018 (US3): Affiliate attribution is preserved ────────────────────────

    [Fact]
    public async Task Register_WithAffiliateSession_AttributesConversion()
    {
        var client = _factory.CreateClient();

        // Step 1: Create an affiliate account and get their referral code
        var affiliateEmail = $"aff.reg_{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", ValidPayload(affiliateEmail, "Aff", "Reg"));

        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = affiliateEmail, password = "Secure@123" });
        var affiliateAuth = await loginResp.Content.ReadFromJsonAsync<AuthResult>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", affiliateAuth!.Token);

        var dashboard = await client.GetFromJsonAsync<DashboardResult>("/api/affiliate/dashboard");
        var affiliateCode = dashboard!.UniqueCode;

        // Step 2: Visitor clicks affiliate link — captures aff_sid cookie
        client.DefaultRequestHeaders.Authorization = null;
        var clickResp = await client.GetAsync($"/api/tracking/click?affiliateCode={affiliateCode}");
        Assert.Equal(HttpStatusCode.OK, clickResp.StatusCode);

        var setCookie = clickResp.Headers.GetValues("Set-Cookie").First();
        var sessionId = setCookie.Split(';')[0].Split('=', 2)[1];

        // Step 3: Visitor registers WITH the aff_sid cookie
        var visitorEmail = $"visitor.aff_{Guid.NewGuid():N}@test.com";
        var visitorRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        visitorRequest.Headers.Add("Cookie", $"aff_sid={sessionId}");
        visitorRequest.Content = JsonContent.Create(ValidPayload(visitorEmail, "Visitor", "Aff"));

        var visitorResp = await client.SendAsync(visitorRequest);
        Assert.Equal(HttpStatusCode.Created, visitorResp.StatusCode);

        // Step 4: Verify conversion event was attributed automatically
        using var scope = _factory.Services.CreateScope();
        var trackingDb = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
        var conversion = await trackingDb.ConversionEvents
            .FirstOrDefaultAsync(e => e.SessionId == sessionId && e.ConversionType == "Registration");

        Assert.NotNull(conversion);
        Assert.Equal("Registration", conversion!.ConversionType);
    }

    // ── T019 (US3): Register then login then access protected endpoint ─────────

    [Fact]
    public async Task Register_ThenLogin_ThenAccessProtectedEndpoint_Returns200()
    {
        var client = _factory.CreateClient();
        var email = $"rll_{Guid.NewGuid():N}@test.com";

        // Register — assert 201 with userId + email (no token)
        var registerResp = await client.PostAsJsonAsync("/api/auth/register",
            ValidPayload(email, "Auth", "Flow"));

        Assert.Equal(HttpStatusCode.Created, registerResp.StatusCode);
        var registerResult = await registerResp.Content.ReadFromJsonAsync<RegisterResult>();
        Assert.NotNull(registerResult);
        Assert.True(registerResult!.UserId > 0);
        Assert.Equal(email, registerResult.Email);

        // Login — assert JWT is returned
        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = email, password = "Secure@123" });

        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var authResult = await loginResp.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(authResult);
        Assert.NotEmpty(authResult!.Token);

        // Use JWT to access protected endpoint (affiliate dashboard)
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResult.Token);
        var dashResp = await client.GetAsync("/api/affiliate/dashboard");

        Assert.Equal(HttpStatusCode.OK, dashResp.StatusCode);
    }
}

