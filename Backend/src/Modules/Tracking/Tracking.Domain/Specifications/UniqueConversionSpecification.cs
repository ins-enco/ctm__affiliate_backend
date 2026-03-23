namespace Tracking.Domain.Specifications;

public class UniqueConversionSpecification(string sessionId, string conversionType) : ISpecification<ConversionEvent>
{
    public Expression<Func<ConversionEvent, bool>> ToExpression()
        => e => e.SessionId == sessionId && e.ConversionType == conversionType;
}
