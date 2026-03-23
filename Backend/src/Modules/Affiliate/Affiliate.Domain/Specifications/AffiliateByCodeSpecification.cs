namespace Affiliate.Domain.Specifications;

public class AffiliateByCodeSpecification(string uniqueCode) : BaseSpecification<AffiliateEntity>(a => a.UniqueCode == uniqueCode);
