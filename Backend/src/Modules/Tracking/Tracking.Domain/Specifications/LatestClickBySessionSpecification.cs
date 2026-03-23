namespace Tracking.Domain.Specifications;

public class LatestClickBySessionSpecification : BaseSpecification<ClickEvent>
{
    public LatestClickBySessionSpecification(string sessionId)
        : base(e => e.SessionId == sessionId)
        => AddOrderByDescending(e => e.ClickedAt);
}
