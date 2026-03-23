namespace Affiliate.Domain.Specifications;

public class AffiliateByIdSpecification(int affiliateId) : BaseSpecification<AffiliateEntity>(a => a.Id == affiliateId);
