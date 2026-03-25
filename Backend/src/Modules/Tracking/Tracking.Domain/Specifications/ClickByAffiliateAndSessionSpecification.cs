namespace Tracking.Domain.Specifications;

public class ClickByAffiliateAndSessionSpecification(int affiliateId, string sessionId)
    : BaseSpecification<ClickEvent>(e => e.AffiliateId == affiliateId && e.SessionId == sessionId);
