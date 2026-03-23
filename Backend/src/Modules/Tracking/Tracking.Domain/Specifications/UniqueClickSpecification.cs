namespace Tracking.Domain.Specifications;

public class UniqueClickSpecification(int affiliateId, string sessionId)
    : BaseSpecification<ClickEvent>(e => e.AffiliateId == affiliateId && e.SessionId == sessionId);
