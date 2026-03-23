namespace CopyTradeMarketApi.Host;

/// <summary>
/// Seeds three developer accounts with realistic click/conversion history.
/// Runs only in Development and is fully idempotent — safe to restart the API.
/// </summary>
public static class DevDataSeeder
{
    // ── Well-known dev accounts ───────────────────────────────────────────────
    // Password is the same for all accounts to keep local dev simple.
    public const string SharedPassword = "DevPass123!";

    private static readonly DevAccount[] Accounts =
    [
        new(Id: 1, Name: "Alice Dev",  Email: "alice@dev.com",  Code: "ALICE001",
            Description: "Active affiliate — many clicks and conversions"),

        new(Id: 2, Name: "Bob Dev",    Email: "bob@dev.com",    Code: "BOB00001",
            Description: "Moderate affiliate — a few clicks, no conversions"),

        new(Id: 3, Name: "Carol Dev",  Email: "carol@dev.com",  Code: "CAROL001",
            Description: "New affiliate — empty stats"),
    ];

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var authDb      = sp.GetRequiredService<AuthDbContext>();
        var affiliateDb = sp.GetRequiredService<AffiliateDbContext>();
        var trackingDb  = sp.GetRequiredService<TrackingDbContext>();

        // Idempotency guard — skip if any dev account already exists
        if (authDb.Users.Any(u => u.Email == Accounts[0].Email))
        {
            logger.LogInformation("Dev seed data already present — skipping");
            return;
        }

        logger.LogInformation("Seeding dev data for {Count} accounts…", Accounts.Length);

        foreach (var account in Accounts)
            await SeedAccountAsync(authDb, affiliateDb, account, logger);

        await SeedClicksAndConversionsAsync(trackingDb, logger);

        logger.LogInformation("Dev seed complete");
    }

    // ── Per-account seeding ───────────────────────────────────────────────────

    private static async Task SeedAccountAsync(
        AuthDbContext authDb, AffiliateDbContext affiliateDb,
        DevAccount account, ILogger logger)
    {
        authDb.Users.Add(new User
        {
            Id           = account.Id,
            Email        = account.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(SharedPassword),
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        });
        await authDb.SaveChangesAsync();

        affiliateDb.Affiliates.Add(new AffiliateEntity
        {
            Id         = account.Id,
            UserId     = account.Id,
            Name       = account.Name,
            UniqueCode = account.Code,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow,
        });
        await affiliateDb.SaveChangesAsync();

        logger.LogInformation("  {Email} → affiliate {Code} ({Description})",
            account.Email, account.Code, account.Description);
    }

    // ── Click & conversion history ────────────────────────────────────────────

    private static async Task SeedClicksAndConversionsAsync(
        TrackingDbContext db, ILogger logger)
    {
        var now = DateTime.UtcNow;

        // Alice (Id=1) — 10 clicks (10 unique), 3 conversions
        db.ClickEvents.AddRange(
            Click(1, "SES-A1", unique: true, days: -1),
            Click(1, "SES-A2", unique: true, days: -2),
            Click(1, "SES-A3", unique: true, days: -3),
            Click(1, "SES-A4", unique: true, days: -4),
            Click(1, "SES-A5", unique: true, days: -5),
            Click(1, "SES-A6", unique: true, days: -6),
            Click(1, "SES-A7", unique: true, days: -9),
            Click(1, "SES-A8", unique: true, days: -12),
            Click(1, "SES-A9", unique: true, days: -20),
            Click(1, "SES-AA", unique: true, days: -30)
        );
        db.ConversionEvents.AddRange(
            Conversion(1, "SES-A1", type: "Registration", days: -1),
            Conversion(1, "SES-A3", type: "Deposit",      days: -3),
            Conversion(1, "SES-A5", type: "Deposit",      days: -5)
        );

        // Bob (Id=2) — 4 clicks (4 unique), 0 conversions
        db.ClickEvents.AddRange(
            Click(2, "SES-B1", unique: true, days: -2),
            Click(2, "SES-B2", unique: true, days: -5),
            Click(2, "SES-B3", unique: true, days: -8),
            Click(2, "SES-B4", unique: true, days: -15)
        );

        // Carol (Id=3) — no history

        await db.SaveChangesAsync();

        logger.LogInformation(
            "  Click/conversion history seeded (Alice: 10 clicks, 3 conversions | Bob: 4 clicks | Carol: 0)");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ClickEvent Click(int affiliateId, string session, bool unique, int days) =>
        new()
        {
            AffiliateId = affiliateId,
            SessionId   = session,
            IsUnique    = unique,
            ClickedAt   = DateTime.UtcNow.AddDays(days),
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow,
        };

    private static ConversionEvent Conversion(int affiliateId, string session, string type, int days) =>
        new()
        {
            AffiliateId    = affiliateId,
            SessionId      = session,
            ConversionType = type,
            ConvertedAt    = DateTime.UtcNow.AddDays(days),
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
        };

    private record DevAccount(int Id, string Name, string Email, string Code, string Description);
}
