namespace Tracking.Application.Tests;

public class ClickStatsReaderTests
{
    private static TrackingDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<TrackingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetAsync_WithMixedClicks_ReturnsCorrectCounts()
    {
        // Arrange
        var db = CreateDbContext();
        var now = DateTime.UtcNow;

        db.ClickEvents.AddRange(
            // Unique, within 7 days
            new ClickEvent { AffiliateId = 1, SessionId = "S1",  ClickedAt = now.AddDays(-1), CreatedAt = now, UpdatedAt = now },
            new ClickEvent { AffiliateId = 1, SessionId = "S2",  ClickedAt = now.AddDays(-3), CreatedAt = now, UpdatedAt = now },
            // Unique, older than 7 days
            new ClickEvent { AffiliateId = 1, SessionId = "S3",  ClickedAt = now.AddDays(-10), CreatedAt = now, UpdatedAt = now },
            // Different affiliate — must not count
            new ClickEvent { AffiliateId = 2, SessionId = "S9",  ClickedAt = now.AddDays(-1), CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();

        var reader = new ClickStatsReader(db);

        // Act
        var stats = await reader.GetAsync(affiliateId: 1);

        // Assert
        Assert.Equal(3, stats.TotalClicks);    // all rows for affiliate 1
        Assert.Equal(3, stats.UniqueClicks);   // IsUnique = true for affiliate 1
        Assert.Equal(2, stats.Last7DayClicks); // unique + within 7 days
        Assert.Equal(0, stats.ConvertedClicks); // no conversions seeded
    }

    [Fact]
    public async Task GetAsync_WithConversions_ReturnsCorrectConvertedCount()
    {
        // Arrange
        var db = CreateDbContext();
        var now = DateTime.UtcNow;

        db.ClickEvents.AddRange(
            new ClickEvent { AffiliateId = 1, SessionId = "S1", ClickedAt = now.AddDays(-1), CreatedAt = now, UpdatedAt = now },
            new ClickEvent { AffiliateId = 1, SessionId = "S2", ClickedAt = now.AddDays(-2), CreatedAt = now, UpdatedAt = now },
            new ClickEvent { AffiliateId = 1, SessionId = "S3", ClickedAt = now.AddDays(-3), CreatedAt = now, UpdatedAt = now }
        );
        db.ConversionEvents.AddRange(
            new ConversionEvent { AffiliateId = 1, SessionId = "S1", ConversionType = "Registration", ConvertedAt = now, CreatedAt = now, UpdatedAt = now },
            new ConversionEvent { AffiliateId = 1, SessionId = "S2", ConversionType = "Deposit",      ConvertedAt = now, CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();

        var reader = new ClickStatsReader(db);

        // Act
        var stats = await reader.GetAsync(affiliateId: 1);

        // Assert — S1 and S2 have conversions, S3 does not
        Assert.Equal(2, stats.ConvertedClicks);
    }

    [Fact]
    public async Task GetAsync_WithNoClicks_ReturnsAllZeros()
    {
        // Arrange
        var db = CreateDbContext();
        var reader = new ClickStatsReader(db);

        // Act
        var stats = await reader.GetAsync(affiliateId: 1);

        // Assert
        Assert.Equal(0, stats.TotalClicks);
        Assert.Equal(0, stats.UniqueClicks);
        Assert.Equal(0, stats.Last7DayClicks);
        Assert.Equal(0, stats.ConvertedClicks);
    }
}
