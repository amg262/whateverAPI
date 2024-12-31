using System.Linq.Expressions;

namespace whateverAPI.Helpers;

public static class QueryHelper
{
    // Provides a standard way to sort any IQueryable by a provided property
    public static IOrderedQueryable<T> ApplySorting<T, TKey>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool descending = false)
    {
        // If descending is true, order by descending, otherwise order ascending
        return descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    // Implements standard pagination logic that can be applied to any IQueryable
    public static IQueryable<T> ApplyPaging<T>(
        IQueryable<T> query,
        int? pageNumber,
        int? pageSize)
    {
        // Only apply paging if both parameters are provided
        if (!pageNumber.HasValue || !pageSize.HasValue)
        {
            return query;
        }

        // Ensure we don't allow negative values
        var page = Math.Max(1, pageNumber.Value);
        var size = Math.Max(1, pageSize.Value);

        return query
            .Skip((page - 1) * size)
            .Take(size);
    }
}