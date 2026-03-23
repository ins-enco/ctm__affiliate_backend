namespace Tracking.Domain.Specifications;

public class ClickBySessionSpecification(string sessionId) : ISpecification<ClickEvent>
{
    public Expression<Func<ClickEvent, bool>> ToExpression()
        => e => e.SessionId == sessionId;
}
