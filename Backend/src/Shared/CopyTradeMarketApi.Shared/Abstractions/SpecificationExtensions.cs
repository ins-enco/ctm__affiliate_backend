using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CopyTradeMarketApi.Shared.Abstractions;

public static class SpecificationExtensions
{
    public static IQueryable<T> Apply<T>(this IQueryable<T> query, ISpecification<T> spec) where T : class
    {
        foreach (var include in spec.Includes)
            query = query.Include(include);

        foreach (var include in spec.IncludeStrings)
            query = query.Include(include);

        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.IsDistinct)
            query = query.Distinct();

        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip).Take(spec.Take);

        return query;
    }
}
