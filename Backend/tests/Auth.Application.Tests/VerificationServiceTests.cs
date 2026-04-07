using Auth.Domain.Entities;
using CopyTradeMarketApi.Shared.Verification;

namespace Auth.Application.Tests;

public class VerificationServiceTests
{
    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options);
    }

    private static IVerificationSettings CreateSettings(int expiryHours = 24)
    {
        var mock = new Mock<IVerificationSettings>();
        mock.Setup(s => s.TokenExpiry).Returns(TimeSpan.FromHours(expiryHours));
        return mock.Object;
    }

    private static async Task<User> SeedUserAsync(AuthDbContext db, bool isVerified = false)
    {
        var user = new User
        {
            Email            = $"user_{Guid.NewGuid():N}@test.com",
            PasswordHash     = "hash",
            IsEmailVerified  = isVerified
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // ── CreateTokenAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTokenAsync_CreatesTokenRecord_WithCorrectFields()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db);
        var service = new VerificationService(db, CreateSettings(24));

        var token = await service.CreateTokenAsync(user.Id, user.Email);

        Assert.NotEmpty(token);
        var record = await db.EmailVerificationTokens.FirstAsync();
        Assert.Equal(user.Id, record.UserId);
        Assert.Equal(user.Email, record.Email);
        Assert.Equal(token, record.Token);
        Assert.Null(record.ConsumedAt);
        Assert.True(record.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateTokenAsync_TokenExpiry_MatchesConfiguredHours()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db);
        var service = new VerificationService(db, CreateSettings(48));

        await service.CreateTokenAsync(user.Id, user.Email);

        var record = await db.EmailVerificationTokens.FirstAsync();
        var diff   = record.ExpiresAt - DateTime.UtcNow;
        Assert.True(diff.TotalHours > 47 && diff.TotalHours <= 49);
    }

    // ── VerifyAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_ValidToken_SetsIsEmailVerifiedAndConsumesToken()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db);
        var service = new VerificationService(db, CreateSettings());
        var token   = await service.CreateTokenAsync(user.Id, user.Email);

        await service.VerifyAsync(token);

        var updatedUser  = await db.Users.FindAsync(user.Id);
        var updatedToken = await db.EmailVerificationTokens.FirstAsync();
        Assert.True(updatedUser!.IsEmailVerified);
        Assert.NotNull(updatedToken.ConsumedAt);
    }

    [Fact]
    public async Task VerifyAsync_ExpiredToken_ThrowsInvalidOperationException()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db);
        var service = new VerificationService(db, CreateSettings());
        var token   = await service.CreateTokenAsync(user.Id, user.Email);

        // Backdate expiry
        var record = await db.EmailVerificationTokens.FirstAsync();
        record.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.VerifyAsync(token));
    }

    [Fact]
    public async Task VerifyAsync_ConsumedToken_ThrowsInvalidOperationException()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db);
        var service = new VerificationService(db, CreateSettings());
        var token   = await service.CreateTokenAsync(user.Id, user.Email);

        await service.VerifyAsync(token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.VerifyAsync(token));
    }

    [Fact]
    public async Task VerifyAsync_AlreadyVerifiedUser_ThrowsConflictException()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db, isVerified: true);
        var service = new VerificationService(db, CreateSettings());
        var token   = await service.CreateTokenAsync(user.Id, user.Email);

        await Assert.ThrowsAsync<ConflictException>(() => service.VerifyAsync(token));
    }

    [Fact]
    public async Task VerifyAsync_UnknownToken_ThrowsInvalidOperationException()
    {
        await using var db = CreateDbContext();
        var service = new VerificationService(db, CreateSettings());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.VerifyAsync("no-such-token"));
    }

    // ── ResendAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResendAsync_Success_InvalidatesOldTokenAndCreatesNew()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db);
        var service = new VerificationService(db, CreateSettings());

        // Issue first token (back-dated to bypass rate limit)
        var first = await service.CreateTokenAsync(user.Id, user.Email);
        var firstRecord = await db.EmailVerificationTokens.FirstAsync();
        firstRecord.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        await db.SaveChangesAsync();

        var secondToken = await service.ResendAsync(user.Email);

        Assert.NotEqual(first, secondToken);
        var firstAfter = await db.EmailVerificationTokens.FindAsync(firstRecord.Id);
        Assert.NotNull(firstAfter!.ConsumedAt); // old token invalidated
    }

    [Fact]
    public async Task ResendAsync_WithinRateLimit_ThrowsTooManyRequestsException()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db);
        var service = new VerificationService(db, CreateSettings());
        await service.CreateTokenAsync(user.Id, user.Email);

        await Assert.ThrowsAsync<TooManyRequestsException>(() => service.ResendAsync(user.Email));
    }

    [Fact]
    public async Task ResendAsync_AlreadyVerified_ThrowsConflictException()
    {
        await using var db = CreateDbContext();
        var user    = await SeedUserAsync(db, isVerified: true);
        var service = new VerificationService(db, CreateSettings());

        await Assert.ThrowsAsync<ConflictException>(() => service.ResendAsync(user.Email));
    }

    [Fact]
    public async Task ResendAsync_EmailNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDbContext();
        var service = new VerificationService(db, CreateSettings());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ResendAsync("nobody@test.com"));
    }
}
