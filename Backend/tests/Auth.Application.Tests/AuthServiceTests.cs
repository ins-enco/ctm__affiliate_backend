namespace Auth.Application.Tests;

public class AuthServiceTests
{
    private static AuthDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static JwtSettings CreateJwtSettings() => new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpiryMinutes = 60,
        SecretKey = "super-secret-key-for-testing-1234567890"
    };

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithNewEmail_ReturnsAuthResultWithToken()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((99, "CODE0001"));

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings());

        // Act
        var result = await service.RegisterAsync(new RegisterRequest("Alice", "alice@test.com", "Password1!"));

        // Assert
        Assert.NotEmpty(result.Token);
        Assert.Equal(99, result.AffiliateId);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((1, "CODE0001"));

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings());
        await service.RegisterAsync(new RegisterRequest("Alice", "alice@test.com", "Password1!"));

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegisterAsync(new RegisterRequest("Alice2", "alice@test.com", "Password2!")));
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsAuthResultWithToken()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((5, "CODE0005"));
        mockLookup.Setup(l => l.GetAffiliateIdByUserIdAsync(It.IsAny<int>()))
                  .ReturnsAsync(5);

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings());
        await service.RegisterAsync(new RegisterRequest("Bob", "bob@test.com", "MyPass123!"));

        // Act
        var result = await service.LoginAsync(new LoginRequest("bob@test.com", "MyPass123!"));

        // Assert
        Assert.NotEmpty(result.Token);
        Assert.Equal(5, result.AffiliateId);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest("nobody@test.com", "password")));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((1, "CODE0001"));

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings());
        await service.RegisterAsync(new RegisterRequest("Carol", "carol@test.com", "RightPass!"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest("carol@test.com", "WrongPass!")));
    }

    [Fact]
    public async Task Login_WithMissingAffiliateProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((1, "CODE0001"));
        // Return null — affiliate profile missing
        mockLookup.Setup(l => l.GetAffiliateIdByUserIdAsync(It.IsAny<int>()))
                  .ReturnsAsync((int?)null);

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings());
        await service.RegisterAsync(new RegisterRequest("Dave", "dave@test.com", "Pass123!"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync(new LoginRequest("dave@test.com", "Pass123!")));
    }
}
