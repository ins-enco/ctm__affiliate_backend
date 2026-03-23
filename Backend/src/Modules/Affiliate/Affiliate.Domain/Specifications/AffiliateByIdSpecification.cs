namespace Affiliate.Domain.Specifications;

public class AffiliateByIdSpecification(int affiliateId) : ISpecification<AffiliateEntity>
{
    public Expression<Func<AffiliateEntity, bool>> ToExpression()
        => a => a.Id == affiliateId;
}
