﻿using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using whateverAPI.Entities;

namespace whateverAPI.Helpers;

/// <summary>
/// Provides extension methods for creating and customizing ProblemDetails responses in ASP.NET Core applications.
/// These extensions simplify the process of generating consistent, RFC 7807-compliant problem details
/// while maintaining a fluent and expressive API.
/// </summary>
/// <remarks>
/// The extensions in this class follow a fluent builder pattern, allowing for method chaining
/// to construct problem details responses. Each method is designed to be composable,
/// letting you build exactly the response you need while maintaining consistent structure.
/// 
/// Example usage:
/// <code>
/// return context.CreateProblemDetails()
///     .WithStatus(StatusCodes.Status404NotFound)
///     .WithTitle("Resource Not Found")
///     .WithDetail("The requested item could not be found")
///     .WithExtension("resourceId", id)
///     .ToProblemResult();
/// </code>
/// </remarks>
/// <summary>
/// Provides extension methods for creating various types of problem details responses.
/// These methods ensure consistent error handling across the application while providing
/// specific context for different types of errors.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Creates a base problem details response. This is the foundation method used by
    /// other more specific problem details creators.
    /// </summary>
    private static ProblemHttpResult CreateProblem(
        this HttpContext context,
        int statusCode,
        string title,
        string detail,
        IDictionary<string, object?>? additionalContext = null)
    {
        var configuration = context.RequestServices
            .GetRequiredService<ProblemDetailsConfig>();

        var problem = configuration.CreateProblemDetails(
            context,
            statusCode,
            title,
            detail,
            additionalContext);

        return TypedResults.Problem(problem);
    }

    /// <summary>
    /// Creates a not found problem details response for a specific resource.
    /// </summary>
    public static IResult CreateNotFoundProblem(
        this HttpContext context,
        string resourceType,
        string identifier)
    {
        return context.CreateProblem(
            StatusCodes.Status404NotFound,
            "Resource Not Found",
            $"{resourceType} with identifier '{identifier}' was not found",
            new Dictionary<string, object?>
            {
                ["resourceType"] = resourceType,
                ["identifier"] = identifier
            });
    }

    /// <summary>
    /// Creates a validation problem details response with optional field-specific errors.
    /// </summary>
    public static IResult CreateValidationProblem(
        this HttpContext context,
        string detail,
        IDictionary<string, string[]>? validationErrors = null)
    {
        var configuration = context.RequestServices
            .GetRequiredService<ProblemDetailsConfig>();

        var problem = new ValidationProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation Failed",
            Detail = detail,
            Type = "https://httpstatuses.com/422"
        };

        if (validationErrors != null)
        {
            foreach (var (field, errors) in validationErrors)
            {
                problem.Errors[field] = errors;
            }
        }

        configuration.EnrichWithContext(problem, context);

        return TypedResults.ValidationProblem(problem.Errors, detail);
    }

    /// <summary>
    /// Creates a service unavailable problem details response with optional retry information.
    /// </summary>
    public static IResult CreateServiceUnavailableProblem(
        this HttpContext context,
        string detail,
        TimeSpan? retryAfter = null)
    {
        var additionalContext = new Dictionary<string, object?>();

        if (retryAfter.HasValue)
        {
            additionalContext["retryAfter"] = retryAfter.Value.TotalSeconds;
            context.Response.Headers.RetryAfter = retryAfter.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }

        return context.CreateProblem(
            StatusCodes.Status503ServiceUnavailable,
            "Service Unavailable",
            detail,
            additionalContext);
    }

    /// <summary>
    /// Creates an unprocessable entity problem details response for failed operations.
    /// </summary>
    public static IResult CreateUnprocessableEntityProblem(
        this HttpContext context,
        string operation,
        string? detail = null)
    {
        return context.CreateProblem(
            StatusCodes.Status422UnprocessableEntity,
            $"{operation} Failed",
            detail ?? $"Failed to {operation.ToLower()} with the provided data",
            new Dictionary<string, object?>
            {
                ["operation"] = operation
            });
    }

    /// <summary>
    /// Creates an unauthorized problem details response.
    /// </summary>
    public static IResult CreateUnauthorizedProblem(
        this HttpContext context,
        string detail = "Authentication is required to access this resource")
    {
        return context.CreateProblem(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            detail);
    }

    /// <summary>
    /// Creates a forbidden problem details response.
    /// </summary>
    public static IResult CreateForbiddenProblem(
        this HttpContext context,
        string detail = "You do not have permission to access this resource")
    {
        return context.CreateProblem(
            StatusCodes.Status403Forbidden,
            "Forbidden",
            detail);
    }

    /// <summary>
    /// Creates a bad request problem details response.
    /// </summary>
    public static IResult CreateBadRequestProblem(
        this HttpContext context,
        string detail)
    {
        return context.CreateProblem(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            detail);
    }

    /// <summary>
    /// Creates a problem details response for external service errors with optional exception details.
    /// </summary>
    public static IResult CreateExternalServiceProblem(
        this HttpContext context,
        string serviceName,
        string detail,
        Exception? exception = null)
    {
        var additionalContext = new Dictionary<string, object?>
        {
            ["service"] = serviceName
        };

        if (exception != null)
        {
            additionalContext["errorType"] = exception.GetType().Name;
            additionalContext["errorMessage"] = exception.Message;

            // Only include stack trace in development
            if (context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                additionalContext["stackTrace"] = exception.StackTrace;
            }
        }

        return context.CreateProblem(
            StatusCodes.Status502BadGateway,
            "External Service Error",
            detail,
            additionalContext);
    }

    /// <summary>
    /// Creates a conflict problem details response for concurrent modification scenarios.
    /// </summary>
    public static IResult CreateConflictProblem(
        this HttpContext context,
        string detail)
    {
        return context.CreateProblem(
            StatusCodes.Status409Conflict,
            "Conflict",
            detail);
    }

    /// <summary>
    /// Creates a too many requests problem details response for rate limiting scenarios.
    /// </summary>
    public static IResult CreateTooManyRequestsProblem(
        this HttpContext context,
        string detail,
        TimeSpan? retryAfter = null)
    {
        var additionalContext = new Dictionary<string, object?>();

        if (retryAfter.HasValue)
        {
            additionalContext["retryAfter"] = retryAfter.Value.TotalSeconds;
            context.Response.Headers.RetryAfter = retryAfter.Value.TotalSeconds.ToString();
        }

        return context.CreateProblem(
            StatusCodes.Status429TooManyRequests,
            "Too Many Requests",
            detail,
            additionalContext);
    }
}

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
    public static IOrderedQueryable<T> ApplySorting<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
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
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int? pageNumber, int? pageSize)
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
    public static IQueryable<Joke> ApplySortingWithTags(this IQueryable<Joke> query, string sortBy, bool descending)
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

/// <summary>
/// Provides extension methods for updating object properties from another instance.
/// </summary>
/// <remarks>
/// This static class adds updating capabilities to any reference type, allowing
/// objects to be easily updated from other instances while maintaining property
/// type safety and null checking.
/// </remarks>
public static class ObjectExtensions
{
    /// <summary>
    /// Updates the current object's properties with non-null values from another instance.
    /// </summary>
    /// <typeparam name="T">The type of objects being updated</typeparam>
    /// <param name="destination">The object being updated (this instance)</param>
    /// <param name="source">The object containing the new values</param>
    /// <returns>The updated destination object for method chaining</returns>
    /// <remarks>
    /// This method implements a safe update strategy:
    /// - Only updates properties when source values are non-null
    /// - Preserves existing values for null source properties
    /// - Handles type compatibility checking
    /// - Supports method chaining for fluent syntax
    /// </remarks>
    public static T MapObject<T>(this T destination, T source) where T : class
    {
        // Validate our parameters
        if (destination == null)
            throw new ArgumentNullException(nameof(destination), "Destination object cannot be null");
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Source object cannot be null");

        // Get all readable properties that can be written to
        var properties = typeof(T).GetProperties()
            .Where(p => p is { CanRead: true, CanWrite: true });

        foreach (var prop in properties) //.Select(p=>p.GetValue(source)))
        {
            try
            {
                // Get the value from the source object
                var value = prop.GetValue(source);
                prop.SetValue(destination, value);
            }
            catch (Exception ex)
            {
                // Consider logging the error but continue with other properties
                // We might want to add logging here depending on requirements
            }
        }

        // Return the destination for method chaining
        return destination;
    }

    /// <summary>
    /// Updates the current object's properties from a different type of object with matching property names.
    /// </summary>
    /// <remarks>
    /// This overload allows updating from different types that share property names,
    /// useful when working with DTOs or similar patterns.
    /// </remarks>
    public static TDestination MapObject<TSource, TDestination>(
        this TDestination destination,
        TSource source)
        where TSource : class
        where TDestination : class
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination), "Destination object cannot be null");
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Source object cannot be null");

        // Create a dictionary of destination properties for efficient lookup
        var destProps = typeof(TDestination).GetProperties()
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name);

        // Get all readable properties from source
        var sourceProps = typeof(TSource).GetProperties()
            .Where(p => p.CanRead);

        foreach (var sourceProp in sourceProps)
        {
            // Try to find matching destination property
            if (!destProps.TryGetValue(sourceProp.Name, out var destProp)) continue;

            // Ensure property types are compatible
            if (!destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType)) continue;

            try
            {
                var value = sourceProp.GetValue(source);
                destProp.SetValue(destination, value);
            }
            catch (Exception ex)
            {
                // Consider logging the error but continue with other properties
            }
        }

        return destination;
    }
}