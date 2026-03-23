namespace Affiliate.Domain.Specifications;

public class AffiliateByUserIdSpecification(int userId) : ISpecification<AffiliateEntity>
{
    public Expression<Func<AffiliateEntity, bool>> ToExpression()
        => a => a.UserId == userId;
}
