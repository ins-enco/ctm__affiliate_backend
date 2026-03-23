namespace Affiliate.Application.Tests;

public class AffiliateDashboardServiceTests
{
    private static AffiliateDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AffiliateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IMemoryCache CreateCache() =>
        new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public async Task GetDashboard_WhenAffiliateFound_ReturnsDashboardResult()
    {
        // Arrange
        var db = CreateDbContext();
        db.Affiliates.Add(new AffiliateEntity
        {
            Id = 1,
            UserId = 10,
            Name = "Alice",
            UniqueCode = "ALICE001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mockReader = new Mock<IClickStatsReader>();
        mockReader.Setup(r => r.GetAsync(1))
                  .ReturnsAsync(new ClickStats(50, 30, 10));

        var service = new AffiliateDashboardService(db, mockReader.Object, CreateCache());

        // Act
        var result = await service.GetDashboardAsync(1);

        // Assert
        Assert.Equal("Alice", result.AffiliateName);
        Assert.Equal("ALICE001", result.UniqueCode);
        Assert.Equal(50, result.TotalClicks);
        Assert.Equal(30, result.UniqueClicks);
        Assert.Equal(10, result.Last7DayClicks);
        Assert.Equal(50, result.CachedClickCount);
    }

    [Fact]
    public async Task GetDashboard_WhenAffiliateNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var db = CreateDbContext();
        var mockReader = new Mock<IClickStatsReader>();
        var service = new AffiliateDashboardService(db, mockReader.Object, CreateCache());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetDashboardAsync(999));
    }
}
