using System.Linq.Expressions;

namespace CopyTradeMarketApi.Shared.Abstractions;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
}
