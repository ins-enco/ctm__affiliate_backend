using System.Linq;

namespace Tracking.Domain.Specifications;

public class ClickWithConversionSpecification : BaseSpecification<ClickEvent>
{
    public ClickWithConversionSpecification(int affiliateId, IQueryable<ConversionEvent> conversions)
        : base(e => e.AffiliateId == affiliateId &&
                    conversions.Any(cv => cv.SessionId == e.SessionId && cv.AffiliateId == affiliateId))
    { }
}
