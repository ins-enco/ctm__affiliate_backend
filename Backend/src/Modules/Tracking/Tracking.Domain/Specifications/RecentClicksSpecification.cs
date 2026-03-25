namespace Tracking.Domain.Specifications;

public class RecentClicksSpecification(int affiliateId, DateTime since)
    : BaseSpecification<ClickEvent>(e => e.AffiliateId == affiliateId && e.ClickedAt >= since);
