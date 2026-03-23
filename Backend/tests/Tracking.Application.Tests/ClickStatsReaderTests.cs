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
            new ClickEvent { AffiliateId = 1, SessionId = "S1", IsUnique = true,  ClickedAt = now.AddDays(-1), CreatedAt = now, UpdatedAt = now },
            new ClickEvent { AffiliateId = 1, SessionId = "S2", IsUnique = true,  ClickedAt = now.AddDays(-3), CreatedAt = now, UpdatedAt = now },
            // Unique, older than 7 days
            new ClickEvent { AffiliateId = 1, SessionId = "S3", IsUnique = true,  ClickedAt = now.AddDays(-10), CreatedAt = now, UpdatedAt = now },
            // Non-unique (repeat visits)
            new ClickEvent { AffiliateId = 1, SessionId = "S1", IsUnique = false, ClickedAt = now.AddDays(-2), CreatedAt = now, UpdatedAt = now },
            // Different affiliate — must not count
            new ClickEvent { AffiliateId = 2, SessionId = "S9", IsUnique = true,  ClickedAt = now.AddDays(-1), CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();

        var reader = new ClickStatsReader(db);

        // Act
        var stats = await reader.GetAsync(affiliateId: 1);

        // Assert
        Assert.Equal(4, stats.TotalClicks);    // all rows for affiliate 1
        Assert.Equal(3, stats.UniqueClicks);   // IsUnique = true for affiliate 1
        Assert.Equal(2, stats.Last7DayClicks); // unique + within 7 days
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
    }
}
