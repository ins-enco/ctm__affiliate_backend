namespace Affiliate.Domain.Specifications;

public class AffiliateByCodeSpecification(string uniqueCode) : ISpecification<AffiliateEntity>
{
    public Expression<Func<AffiliateEntity, bool>> ToExpression()
        => a => a.UniqueCode == uniqueCode;
}
