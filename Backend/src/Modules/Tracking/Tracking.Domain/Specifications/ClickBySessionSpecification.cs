namespace Tracking.Domain.Specifications;

public class ClickBySessionSpecification(string sessionId) : BaseSpecification<ClickEvent>(e => e.SessionId == sessionId);
