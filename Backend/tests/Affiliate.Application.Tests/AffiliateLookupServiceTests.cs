using Xunit;
using Affiliate.Application.Services;
using Affiliate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AffiliateEntity = Affiliate.Domain.Entities.Affiliate;

namespace Affiliate.Application.Tests;

public class AffiliateLookupServiceTests
{
    private static AffiliateDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AffiliateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task CreateAffiliate_ReturnsNewAffiliateIdAndUniqueCode()
    {
        // Arrange
        var db = CreateDbContext();
        var service = new AffiliateLookupService(db);

        // Act
        var (affiliateId, uniqueCode) = await service.CreateAffiliateAsync(userId: 1, name: "Bob");

        // Assert
        Assert.True(affiliateId > 0);
        Assert.Equal(8, uniqueCode.Length);
        Assert.Equal(1, await db.Affiliates.CountAsync());
    }

    [Fact]
    public async Task GetAffiliateIdByUserId_WhenFound_ReturnsAffiliateId()
    {
        // Arrange
        var db = CreateDbContext();
        db.Affiliates.Add(new AffiliateEntity
        {
            Id = 1,
            UserId = 42,
            Name = "Carol",
            UniqueCode = "CAROL001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new AffiliateLookupService(db);

        // Act
        var result = await service.GetAffiliateIdByUserIdAsync(42);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task GetAffiliateIdByUserId_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var db = CreateDbContext();
        var service = new AffiliateLookupService(db);

        // Act
        var result = await service.GetAffiliateIdByUserIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByCode_WhenFound_ReturnsAffiliateInfo()
    {
        // Arrange
        var db = CreateDbContext();
        db.Affiliates.Add(new AffiliateEntity
        {
            Id = 5,
            UserId = 1,
            Name = "Dave",
            UniqueCode = "DAVE1234",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new AffiliateLookupService(db);

        // Act
        var result = await service.FindByCodeAsync("DAVE1234");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Value.affiliateId);
        Assert.Equal("DAVE1234", result.Value.uniqueCode);
    }

    [Fact]
    public async Task FindByCode_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var db = CreateDbContext();
        var service = new AffiliateLookupService(db);

        // Act
        var result = await service.FindByCodeAsync("UNKNOWN1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindById_WhenFound_ReturnsAffiliateInfo()
    {
        // Arrange
        var db = CreateDbContext();
        db.Affiliates.Add(new AffiliateEntity
        {
            Id = 7,
            UserId = 3,
            Name = "Eve",
            UniqueCode = "EVE12345",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new AffiliateLookupService(db);

        // Act
        var result = await service.FindByIdAsync(7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.Value.affiliateId);
        Assert.Equal("EVE12345", result.Value.uniqueCode);
    }

    [Fact]
    public async Task FindById_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var db = CreateDbContext();
        var service = new AffiliateLookupService(db);

        // Act
        var result = await service.FindByIdAsync(999);

        // Assert
        Assert.Null(result);
    }
}
