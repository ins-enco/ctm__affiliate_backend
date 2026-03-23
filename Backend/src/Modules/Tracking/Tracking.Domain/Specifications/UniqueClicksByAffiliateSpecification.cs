namespace Tracking.Domain.Specifications;

public class UniqueClicksByAffiliateSpecification(int affiliateId)
    : BaseSpecification<ClickEvent>(e => e.AffiliateId == affiliateId && e.IsUnique);
