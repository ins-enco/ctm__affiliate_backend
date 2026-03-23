namespace Affiliate.Application.Services;

public class AffiliateLookupService(AffiliateDbContext db) : IAffiliateLookupService
{
    public async Task<(int affiliateId, string uniqueCode)> CreateAffiliateAsync(int userId, string name)
    {
        var code = await GenerateUniqueCodeAsync();
        var affiliate = new AffiliateEntity
        {
            UserId = userId,
            Name = name,
            UniqueCode = code
        };
        db.Affiliates.Add(affiliate);
        await db.SaveChangesAsync();
        return (affiliate.Id, affiliate.UniqueCode);
    }

    public async Task<int?> GetAffiliateIdByUserIdAsync(int userId)
    {
        var affiliate = await db.Affiliates.FirstOrDefaultAsync(a => a.UserId == userId);
        return affiliate?.Id;
    }

    public async Task<(int affiliateId, string uniqueCode)?> FindByCodeAsync(string affiliateCode)
    {
        var affiliate = await db.Affiliates.FirstOrDefaultAsync(a => a.UniqueCode == affiliateCode);
        if (affiliate is null) return null;
        return (affiliate.Id, affiliate.UniqueCode);
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 8)
                .Select(_ => chars[Random.Shared.Next(chars.Length)])
                .ToArray());

            if (!await db.Affiliates.AnyAsync(a => a.UniqueCode == code))
                return code;
        }
    }
}
