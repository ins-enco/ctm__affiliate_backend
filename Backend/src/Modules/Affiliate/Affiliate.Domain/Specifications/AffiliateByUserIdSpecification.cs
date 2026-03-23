namespace Affiliate.Domain.Specifications;

public class AffiliateByUserIdSpecification(int userId) : BaseSpecification<AffiliateEntity>(a => a.UserId == userId);
