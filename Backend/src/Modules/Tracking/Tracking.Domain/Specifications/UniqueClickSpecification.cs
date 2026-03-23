namespace Tracking.Domain.Specifications;

public class UniqueClickSpecification(int affiliateId, string sessionId) : ISpecification<ClickEvent>
{
    public Expression<Func<ClickEvent, bool>> ToExpression()
        => e => e.AffiliateId == affiliateId && e.SessionId == sessionId;
}
