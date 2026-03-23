namespace Tracking.Domain.Specifications;

public class UniqueConversionSpecification(string sessionId, string conversionType)
    : BaseSpecification<ConversionEvent>(e => e.SessionId == sessionId && e.ConversionType == conversionType);
