using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using whateverAPI.Entities;

namespace whateverAPI.Helpers;

/// <summary>
/// Provides extension methods for building efficient and flexible database queries, focusing on 
/// common operations like sorting and pagination while maintaining optimal performance.
/// </summary>
/// <remarks>
/// This helper class implements query composition patterns that follow Entity Framework best practices.
/// It's designed to build queries that translate efficiently to SQL while providing a clean, 
/// reusable interface for common data access patterns.
/// 
/// The class supports three main query enhancement categories:
/// 1. Basic sorting with flexible direction control
/// 2. Standard pagination with safety checks
/// 3. Complex tag-based sorting for hierarchical data
/// 
/// Key benefits of using this helper:
/// - Queries remain IQueryable until materialized, preserving efficient database operations
/// - Consistent handling of null and edge cases
/// - Composable query building that maintains clean SQL translation
/// - Prevention of common performance pitfalls
/// </remarks>
public static class QueryHelper
{
    /// <summary>
    /// Applies flexible sorting to a query based on a provided key selector expression.
    /// </summary>
    /// <typeparam name="T">The type of entities in the query</typeparam>
    /// <typeparam name="TKey">The type of the sorting key</typeparam>
    /// <param name="query">The source query to sort</param>
    /// <param name="keySelector">An expression defining the sorting key</param>
    /// <param name="descending">Optional flag to reverse the sort order</param>
    /// <returns>A new IOrderedQueryable with sorting applied</returns>
    /// <remarks>
    /// This method provides a foundation for building sorted queries with several important features:
    /// 
    /// Query Composition:
    /// - Maintains IQueryable interface for delayed execution
    /// - Preserves the ability to add additional query operations
    /// - Translates efficiently to SQL ORDER BY clauses
    /// 
    /// Usage Example:
    /// <code>
    /// var query = dbContext.Items.AsQueryable();
    /// var sortedQuery = ApplySorting(query, item => item.Name, descending: true);
    /// </code>
    /// </remarks>
    public static IOrderedQueryable<T> ApplySorting<T, TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector,
        bool? descending = false)
    {
        descending ??= false;
        return (bool)descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    /// <summary>
    /// Implements standardized pagination logic that can be safely applied to any queryable sequence.
    /// </summary>
    /// <typeparam name="T">The type of entities in the query</typeparam>
    /// <param name="query">The source query to paginate</param>
    /// <param name="pageNumber">The 1-based page number to retrieve</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>A new IQueryable representing the requested page of data</returns>
    /// <remarks>
    /// This method implements robust pagination with several safety features:
    /// 
    /// Safety Checks:
    /// - Validates and corrects negative page numbers
    /// - Ensures minimum page size of 1
    /// - Handles null parameters gracefully
    /// 
    /// Usage Example:
    /// <code>
    /// var query = dbContext.Items.AsQueryable();
    /// var pagedQuery = ApplyPaging(query, pageNumber: 2, pageSize: 20);
    /// </code>
    /// </remarks>
    public static IQueryable<T> ApplyPaging<T>(IQueryable<T> query, int? pageNumber, int? pageSize)
    {
        // Only apply paging if both parameters are provided
        if (!pageNumber.HasValue || !pageSize.HasValue) return query;
        
        var page = Math.Max(1, pageNumber.Value);
        var size = Math.Max(1, pageSize.Value);

        return query
            .Skip((page - 1) * size)
            .Take(size);
    }

    /// <summary>
    /// Provides sophisticated sorting capabilities for jokes based on their tags, handling the complexity
    /// of sorting entities with collections while maintaining query efficiency.
    /// </summary>
    /// <param name="query">The source query containing jokes</param>
    /// <param name="sortBy">The field to sort by, with special handling for tag-based sorting</param>
    /// <param name="descending">Whether to sort in descending order</param>
    /// <returns>An ordered query of jokes</returns>
    /// <remarks>
    /// This method implements an advanced sorting strategy that handles both simple fields and
    /// complex tag relationships. It's particularly notable for its handling of tag-based sorting:
    /// 
    /// Tag Sorting Strategy:
    /// 1. Determines the maximum number of tags across all jokes
    /// 2. Creates a deterministic ordering of tags within each joke
    /// 3. Applies multi-level sorting based on tag positions
    /// 
    /// Example Usage:
    /// <code>
    /// var query = dbContext.Jokes.Include(j => j.Tags);
    /// var orderedJokes = ApplySortingWithTags(query, "tag", descending: false);
    /// </code>
    /// </remarks>
    public static IQueryable<Joke> ApplySortingWithTags(IQueryable<Joke> query, string sortBy, bool descending)
    {
        if (sortBy.StartsWith("tag", StringComparison.OrdinalIgnoreCase))
        {
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

        // For other fields use the existing logic
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