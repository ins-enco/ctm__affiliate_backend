namespace Tracking.Domain.Specifications;

public class RecentUniqueClicksSpecification(int affiliateId, DateTime since)
    : BaseSpecification<ClickEvent>(e => e.AffiliateId == affiliateId && e.IsUnique && e.ClickedAt >= since);
