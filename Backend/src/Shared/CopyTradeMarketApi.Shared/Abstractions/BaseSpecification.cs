using System.Linq.Expressions;

namespace CopyTradeMarketApi.Shared.Abstractions;

public abstract class BaseSpecification<T> : ISpecification<T>
{
    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    protected BaseSpecification() { }

    public Expression<Func<T, bool>>? Criteria { get; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public bool IsDistinct { get; private set; }
    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
        => Includes.Add(includeExpression);

    protected void AddInclude(string includeString)
        => IncludeStrings.Add(includeString);

    protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        => OrderBy = orderByExpression;

    protected void AddOrderByDescending(Expression<Func<T, object>> orderByExpression)
        => OrderByDescending = orderByExpression;

    protected void ApplyDistinct()
        => IsDistinct = true;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}

public abstract class BaseSpecification<T, TResult> : BaseSpecification<T>, ISpecification<T, TResult>
{
    protected BaseSpecification() { }

    protected BaseSpecification(Expression<Func<T, bool>> criteria) : base(criteria) { }

    public Expression<Func<T, TResult>>? Select { get; private set; }

    protected void AddSelect(Expression<Func<T, TResult>> selectExpression)
        => Select = selectExpression;
}
