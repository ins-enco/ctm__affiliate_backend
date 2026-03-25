namespace Tracking.Domain.Specifications;

public class ConversionBySessionAndTypeSpecification(string sessionId, string conversionType)
    : BaseSpecification<ConversionEvent>(e => e.SessionId == sessionId && e.ConversionType == conversionType);
