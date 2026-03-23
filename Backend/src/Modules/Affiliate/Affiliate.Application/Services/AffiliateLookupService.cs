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
        var affiliate = await db.Affiliates
            .Apply(new AffiliateByUserIdSpecification(userId))
            .FirstOrDefaultAsync();
        return affiliate?.Id;
    }

    public async Task<(int affiliateId, string uniqueCode)?> FindByCodeAsync(string affiliateCode)
    {
        var affiliate = await db.Affiliates
            .Apply(new AffiliateByCodeSpecification(affiliateCode))
            .FirstOrDefaultAsync();
        if (affiliate is null) return null;
        return (affiliate.Id, affiliate.UniqueCode);
    }

    public async Task<(int affiliateId, string uniqueCode)?> FindByIdAsync(int affiliateId)
    {
        var affiliate = await db.Affiliates
            .Apply(new AffiliateByIdSpecification(affiliateId))
            .FirstOrDefaultAsync();
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

            if (!await db.Affiliates.Apply(new AffiliateByCodeSpecification(code)).AnyAsync())
                return code;
        }
    }
}
