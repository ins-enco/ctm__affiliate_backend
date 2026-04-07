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

    private static IEventPublisher CreateEventPublisher() =>
        new Mock<IEventPublisher>().Object;

    private static RegisterRequest ValidRegisterRequest(string email = "alice@test.com") =>
        new()
        {
            UserInformation = new UserInformationDto
            {
                FirstName   = "Alice",
                LastName    = "Test",
                Email       = email,
                PhoneCode   = "+84",
                PhoneNumber = "901234567",
                Language    = "vi"
            },
            Password        = "Password1!",
            ConfirmPassword = "Password1!"
        };

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithNewEmail_ReturnsRegisterResultWithUserIdAndEmail()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((99, "CODE0001"));

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings(), CreateEventPublisher());

        // Act
        var result = await service.RegisterAsync(ValidRegisterRequest());

        // Assert
        Assert.True(result.UserId > 0);
        Assert.Equal("alice@test.com", result.Email);
    }

    [Fact]
    public async Task Register_WithValidRequest_CreatesUserWithAllProfileFields()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((99, "CODE0001"));

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings(), CreateEventPublisher());

        // Act
        var result = await service.RegisterAsync(ValidRegisterRequest());

        // Assert — all profile fields persisted
        var user = await db.Users
            .Include(u => u.Information)
            .FirstAsync(u => u.Email == "alice@test.com");

        Assert.NotNull(user.Information);
        Assert.Equal("Alice",       user.Information!.FirstName);
        Assert.Equal("Test",        user.Information.LastName);
        Assert.Equal("+84",         user.Information.PhoneCode);
        Assert.Equal("901234567",   user.Information.PhoneNumber);
        Assert.Equal("vi",          user.Information.Language);

        // Register returns userId + email — no token
        Assert.True(result.UserId > 0);
        Assert.Equal("alice@test.com", result.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((1, "CODE0001"));

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings(), CreateEventPublisher());
        await service.RegisterAsync(ValidRegisterRequest());

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegisterAsync(ValidRegisterRequest()));
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

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings(), CreateEventPublisher());
        await service.RegisterAsync(ValidRegisterRequest("bob@test.com") with
        {
            UserInformation = new UserInformationDto
            {
                FirstName   = "Bob",
                LastName    = "Test",
                Email       = "bob@test.com",
                PhoneCode   = "+84",
                PhoneNumber = "901234567",
                Language    = "en"
            },
            Password        = "MyPass123!",
            ConfirmPassword = "MyPass123!"
        });

        // Act
        var result = await service.LoginAsync(new LoginRequest { Email = "bob@test.com", Password = "MyPass123!" });

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
        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings(), CreateEventPublisher());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest { Email = "nobody@test.com", Password = "password" }));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.CreateAffiliateAsync(It.IsAny<int>(), It.IsAny<string>()))
                  .ReturnsAsync((1, "CODE0001"));

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings(), CreateEventPublisher());
        await service.RegisterAsync(ValidRegisterRequest("carol@test.com") with
        {
            UserInformation = new UserInformationDto
            {
                FirstName   = "Carol",
                LastName    = "Test",
                Email       = "carol@test.com",
                PhoneCode   = "+84",
                PhoneNumber = "901234567",
                Language    = "en"
            },
            Password        = "RightPass!",
            ConfirmPassword = "RightPass!"
        });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest { Email = "carol@test.com", Password = "WrongPass!" }));
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

        var service = new AuthService(db, mockLookup.Object, CreateJwtSettings(), CreateEventPublisher());
        await service.RegisterAsync(ValidRegisterRequest("dave@test.com") with
        {
            UserInformation = new UserInformationDto
            {
                FirstName   = "Dave",
                LastName    = "Test",
                Email       = "dave@test.com",
                PhoneCode   = "+84",
                PhoneNumber = "901234567",
                Language    = "en"
            },
            Password        = "Pass123!",
            ConfirmPassword = "Pass123!"
        });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync(new LoginRequest { Email = "dave@test.com", Password = "Pass123!" }));
    }
}
