using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests;

/// <summary>
/// Seeds a deterministic set of data so scenario tests can rely on known IDs,
/// codes, and statistics without having to create them through the API first.
/// </summary>
public static class TestDataSeeder
{
    // Well-known constants tests can reference
    public const int    SeededUserId      = 100;
    public const int    SeededAffiliateId = 100;
    public const string SeededEmail       = "seeded@test.com";
    public const string SeededPassword    = "SeededPass1!";
    public const string SeededCode        = "SEED0001";

    /// <summary>
    /// Seeds:
    /// - 1 user  (SeededUserId / SeededEmail / SeededPassword)
    /// - 1 affiliate (SeededAffiliateId / SeededCode)
    /// - 5 click events: 4 unique, 3 of those within the last 7 days
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        await SeedAuthAsync(sp);
        await SeedAffiliateAsync(sp);
        await SeedClicksAsync(sp);
    }

    private static async Task SeedAuthAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AuthDbContext>();
        db.Users.Add(new User
        {
            Id           = SeededUserId,
            Email        = SeededEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(SeededPassword),
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedAffiliateAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AffiliateDbContext>();
        db.Affiliates.Add(new AffiliateEntity
        {
            Id         = SeededAffiliateId,
            UserId     = SeededUserId,
            Name       = "Seeded Affiliate",
            UniqueCode = SeededCode,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedClicksAsync(IServiceProvider sp)
    {
        var db  = sp.GetRequiredService<TrackingDbContext>();
        var now = DateTime.UtcNow;

        db.ClickEvents.AddRange(
            // 3 unique clicks within the last 7 days
            new ClickEvent { AffiliateId = SeededAffiliateId, SessionId = "SES-A",  ClickedAt = now.AddDays(-1),  CreatedAt = now, UpdatedAt = now },
            new ClickEvent { AffiliateId = SeededAffiliateId, SessionId = "SES-B",  ClickedAt = now.AddDays(-3),  CreatedAt = now, UpdatedAt = now },
            new ClickEvent { AffiliateId = SeededAffiliateId, SessionId = "SES-C",  ClickedAt = now.AddDays(-5),  CreatedAt = now, UpdatedAt = now },
            // 1 unique click older than 7 days
            new ClickEvent { AffiliateId = SeededAffiliateId, SessionId = "SES-D",  ClickedAt = now.AddDays(-10), CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();
    }
}
