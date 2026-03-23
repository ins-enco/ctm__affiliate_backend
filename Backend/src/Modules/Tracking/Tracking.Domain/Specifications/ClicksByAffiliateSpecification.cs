namespace Tracking.Domain.Specifications;

public class ClicksByAffiliateSpecification(int affiliateId)
    : BaseSpecification<ClickEvent>(e => e.AffiliateId == affiliateId);
