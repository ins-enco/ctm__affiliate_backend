namespace Tracking.Application.Tests;

public class TrackingServiceTests
{
    private static TrackingDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<TrackingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Mock<ICacheService> CreateCacheMock()
    {
        var mock = new Mock<ICacheService>();
        mock.Setup(c => c.Remove(It.IsAny<string>()));
        mock.Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<(int, string)?>>>(),
                It.IsAny<TimeSpan>()))
            .Returns<string, Func<Task<(int, string)?>>, TimeSpan>((_, factory, _) => factory());
        return mock;
    }

    // ── RecordClick ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordClick_NewSession_SavesClickAndReturnsIsUniqueTrue()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.FindByCodeAsync("AFF00001"))
                  .ReturnsAsync((affiliateId: 1, uniqueCode: "AFF00001"));

        var service = new TrackingService(db, mockLookup.Object, CreateCacheMock().Object);

        // Act
        var result = await service.RecordClickAsync("AFF00001", "1.2.3.4", "Mozilla", existingSessionId: null);

        // Assert
        Assert.True(result.IsUnique);
        Assert.Equal("AFF00001", result.AffiliateCode);
        Assert.Equal(1, await db.ClickEvents.CountAsync());
    }

    [Fact]
    public async Task RecordClick_UnknownAffiliateCode_ThrowsKeyNotFoundException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.FindByCodeAsync(It.IsAny<string>()))
                  .ReturnsAsync((ValueTuple<int, string>?)null);

        var service = new TrackingService(db, mockLookup.Object, CreateCacheMock().Object);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RecordClickAsync("BADCODE1", "1.2.3.4", "Agent", existingSessionId: null));
    }

    // Note: duplicate-click test (DbUpdateException path) requires a real DB with unique index enforcement.
    // EF Core InMemory provider does not enforce unique constraints — covered in integration tests instead.

    // ── RecordConversion ──────────────────────────────────────────────────────

    [Fact]
    public async Task RecordConversion_WithMatchingClick_ReturnsAttributed()
    {
        // Arrange
        var db = CreateDbContext();
        db.ClickEvents.Add(new ClickEvent
        {
            AffiliateId = 3,
            SessionId = "SESSION-B",
            ClickedAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mockLookup = new Mock<IAffiliateLookupService>();
        mockLookup.Setup(l => l.FindByIdAsync(3))
                  .ReturnsAsync((affiliateId: 3, uniqueCode: "AFF00003"));

        var service = new TrackingService(db, mockLookup.Object, CreateCacheMock().Object);

        // Act
        var result = await service.RecordConversionAsync(
            new ConversionRequest("SESSION-B", "Registration", UserId: null));

        // Assert
        Assert.True(result.IsAttributed);
        Assert.Equal("AFF00003", result.AffiliateCode);
        Assert.Equal("Registration", result.ConversionType);
    }

    [Fact]
    public async Task RecordConversion_WithNoMatchingClick_ReturnsUnattributed()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        var service = new TrackingService(db, mockLookup.Object, CreateCacheMock().Object);

        // Act
        var result = await service.RecordConversionAsync(
            new ConversionRequest("SESSION-NONE", "Deposit", UserId: null));

        // Assert
        Assert.False(result.IsAttributed);
        Assert.Null(result.AffiliateCode);
        Assert.Equal(1, await db.ConversionEvents.CountAsync());
    }

    [Fact]
    public async Task RecordConversion_DuplicateForSameSession_ThrowsConflictException()
    {
        // Arrange
        var db = CreateDbContext();
        db.ConversionEvents.Add(new ConversionEvent
        {
            AffiliateId = 0,
            SessionId = "SESSION-C",
            ConversionType = "Registration",
            ConvertedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mockLookup = new Mock<IAffiliateLookupService>();
        var service = new TrackingService(db, mockLookup.Object, CreateCacheMock().Object);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.RecordConversionAsync(new ConversionRequest("SESSION-C", "Registration", UserId: null)));
    }

    [Fact]
    public async Task RecordConversion_InvalidConversionType_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockLookup = new Mock<IAffiliateLookupService>();
        var service = new TrackingService(db, mockLookup.Object, CreateCacheMock().Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordConversionAsync(new ConversionRequest("SESSION-D", "Withdrawal", UserId: null)));
    }
}
