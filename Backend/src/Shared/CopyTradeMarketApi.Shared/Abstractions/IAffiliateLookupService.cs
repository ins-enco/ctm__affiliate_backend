namespace CopyTradeMarketApi.Shared.Abstractions;

public interface IAffiliateLookupService
{
    Task<(int affiliateId, string uniqueCode)> CreateAffiliateAsync(int userId, string name);
    Task<int?> GetAffiliateIdByUserIdAsync(int userId);
    Task<(int affiliateId, string uniqueCode)?> FindByCodeAsync(string affiliateCode);
}
