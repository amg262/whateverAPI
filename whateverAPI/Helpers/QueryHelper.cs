using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using whateverAPI.Entities;

namespace whateverAPI.Helpers;

public static class QueryHelper
{
    // Provides a standard way to sort any IQueryable by a provided property
    public static IOrderedQueryable<T> ApplySorting<T, TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector,
        bool descending = false)
    {
        // If descending is true, order by descending, otherwise order ascending
        return descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    public static IOrderedQueryable<T> ApplySorting<T, TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector,
        bool? descending = false)
    {
        descending ??= false;
        // If descending is true, order by descending, otherwise order ascending
        return (bool)descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    // Implements standard pagination logic that can be applied to any IQueryable
    public static IQueryable<T> ApplyPaging<T>(IQueryable<T> query, int? pageNumber, int? pageSize)
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

    public static IQueryable<Joke> ApplySortingWithTags(IQueryable<Joke> query, string sortBy, bool descending)
    {
        if (sortBy.Equals("tag", StringComparison.OrdinalIgnoreCase))
        {
            // First, let's handle the base case where there are no jokes
            if (!query.Any()) return query;

            // Find the maximum number of tags across all jokes
            // We do this in a separate query to optimize performance
            var maxTagCount = query
                .Select(j => j.Tags.Count)
                .Max();

            // Start with the first sorting operation

            var orderedQuery =
                // Initial ordering (first tag)
                descending
                    ? query.OrderByDescending(j => j.Tags
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .FirstOrDefault() ?? "")
                    : query.OrderBy(j => j.Tags
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .FirstOrDefault() ?? "");

            // Then add subsequent orderings for each potential tag position
            // Start from 1 since we've already handled the first position
            for (var i = 1; i < maxTagCount; i++)
            {
                var index = i; // Capture the index for use in the lambda
                orderedQuery = descending
                    ? orderedQuery.ThenByDescending(j => j.Tags
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .Skip(index)
                        .FirstOrDefault() ?? "")
                    : orderedQuery.ThenBy(j => j.Tags
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .Skip(index)
                        .FirstOrDefault() ?? "");
            }

            return orderedQuery;
        }

        // For other fields, use the existing logic
        Expression<Func<Joke, object>> keySelector = sortBy.ToLower().Trim() switch
        {
            "createdat" => j => j.CreatedAt,
            "modifiedat" => j => j.ModifiedAt,
            "laughscore" => j => j.LaughScore ?? 0,
            "content" => j => j.Content,
            _ => j => j.CreatedAt
        };

        return ApplySorting(query, keySelector, descending);
    }
}