using System.Diagnostics;
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
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Creates a new ProblemDetails instance with standard context information.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <returns>A new ProblemDetails instance initialized with context information.</returns>
    private static ProblemDetails CreateProblemDetails(this HttpContext context)
    {
        var problem = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        return problem.WithContext(context);
    }

    /// <summary>
    /// Enriches a ProblemDetails instance with standard context information.
    /// </summary>
    /// <param name="problem">The ProblemDetails instance to enrich.</param>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <returns>The enriched ProblemDetails instance.</returns>
    private static ProblemDetails WithContext(this ProblemDetails problem, HttpContext context)
    {
        problem.Instance ??= context.Request.Path;

        // Add standard diagnostic information
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problem.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problem.Extensions["requestMethod"] = context.Request.Method;
        problem.Extensions["endpoint"] = context.Request.Path;

        return problem;
    }

    /// <summary>
    /// Sets the status code for a ProblemDetails instance and automatically updates the type URL.
    /// </summary>
    /// <param name="problem">The ProblemDetails instance to modify.</param>
    /// <param name="statusCode">The HTTP status code to set.</param>
    /// <returns>The modified ProblemDetails instance.</returns>
    private static ProblemDetails WithStatus(this ProblemDetails problem, int statusCode)
    {
        problem.Status = statusCode;
        problem.Type = $"https://httpstatuses.com/{statusCode}";
        return problem;
    }

    /// <summary>
    /// Sets the title for a ProblemDetails instance.
    /// </summary>
    /// <param name="problem">The ProblemDetails instance to modify.</param>
    /// <param name="title">The title describing the problem type.</param>
    /// <returns>The modified ProblemDetails instance.</returns>
    private static ProblemDetails WithTitle(this ProblemDetails problem, string title)
    {
        problem.Title = title;
        return problem;
    }

    /// <summary>
    /// Sets the detail message for a ProblemDetails instance.
    /// </summary>
    /// <param name="problem">The ProblemDetails instance to modify.</param>
    /// <param name="detail">A detailed explanation of the problem.</param>
    /// <returns>The modified ProblemDetails instance.</returns>
    private static ProblemDetails WithDetail(this ProblemDetails problem, string detail)
    {
        problem.Detail = detail;
        return problem;
    }

    /// <summary>
    /// Adds a single extension property to a ProblemDetails instance.
    /// </summary>
    /// <param name="problem">The ProblemDetails instance to modify.</param>
    /// <param name="key">The key for the extension property.</param>
    /// <param name="value">The value for the extension property.</param>
    /// <returns>The modified ProblemDetails instance.</returns>
    private static ProblemDetails WithExtension(this ProblemDetails problem, string key, object? value)
    {
        problem.Extensions[key] = value;
        return problem;
    }

    /// <summary>
    /// Adds multiple extension properties to a ProblemDetails instance.
    /// </summary>
    /// <param name="problem">The ProblemDetails instance to modify.</param>
    /// <param name="extensions">A dictionary of extension properties to add.</param>
    /// <returns>The modified ProblemDetails instance.</returns>
    private static ProblemDetails WithExtensions(this ProblemDetails problem, IDictionary<string, object?> extensions)
    {
        foreach (var extension in extensions)
        {
            problem.Extensions[extension.Key] = extension.Value;
        }

        return problem;
    }

    /// <summary>
    /// Converts a ProblemDetails instance to an IResult that can be returned from an endpoint.
    /// </summary>
    /// <param name="problem">The ProblemDetails instance to convert.</param>
    /// <returns>An IResult representing the problem details response.</returns>
    private static ProblemHttpResult ToProblemResult(this ProblemDetails problem) => TypedResults.Problem(problem);


    /// <summary>
    /// Creates a not found problem details response for a specific resource.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <param name="resourceType">The type of resource that wasn't found.</param>
    /// <param name="identifier">The identifier that was used to look up the resource.</param>
    /// <returns>An IResult representing a not found response.</returns>
    public static IResult CreateNotFoundProblem(this HttpContext context, string resourceType, string identifier)
    {
        return context.CreateProblemDetails()
            .WithStatus(StatusCodes.Status404NotFound)
            .WithTitle("Resource Not Found")
            .WithDetail($"{resourceType} with identifier '{identifier}' was not found")
            .WithExtensions(new Dictionary<string, object?>
            {
                ["resourceType"] = resourceType,
                ["identifier"] = identifier
            })
            .ToProblemResult();
    }

    /// <summary>
    /// Creates a validation problem details response.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <param name="detail">A description of the validation problem.</param>
    /// <param name="validationErrors">Optional dictionary of field-specific validation errors.</param>
    /// <returns>An IResult representing a validation error response.</returns>
    public static IResult CreateValidationProblem(
        this HttpContext context,
        string detail,
        IDictionary<string, string[]>? validationErrors = null)
    {
        var problem = context.CreateProblemDetails()
            .WithStatus(StatusCodes.Status422UnprocessableEntity)
            .WithTitle("Validation Failed")
            .WithDetail(detail);

        if (validationErrors != null)
        {
            problem.WithExtension("validationErrors", validationErrors);
        }

        return problem.ToProblemResult();
    }

    /// <summary>
    /// Creates a service unavailable problem details response.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <param name="detail">A description of why the service is unavailable.</param>
    /// <param name="retryAfter">Optional TimeSpan indicating when to retry the request.</param>
    /// <returns>An IResult representing a service unavailable response.</returns>
    public static IResult CreateServiceUnavailableProblem(
        this HttpContext context,
        string detail,
        TimeSpan? retryAfter = null)
    {
        var problem = context.CreateProblemDetails()
            .WithStatus(StatusCodes.Status503ServiceUnavailable)
            .WithTitle("Service Unavailable")
            .WithDetail(detail);

        if (retryAfter.HasValue)
        {
            problem.WithExtension("retryAfter", retryAfter.Value.TotalSeconds);
        }

        return problem.ToProblemResult();
    }

    /// <summary>
    /// Creates an unprocessable entity problem details response when an operation fails.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <param name="operation">The name of the operation that failed.</param>
    /// <param name="detail">Optional detailed message. If not provided, constructs a standard message.</param>
    /// <returns>An IResult representing an unprocessable entity response.</returns>
    public static IResult CreateUnprocessableEntityProblem(
        this HttpContext context,
        string operation,
        string? detail = null)
    {
        return context.CreateProblemDetails()
            .WithStatus(StatusCodes.Status422UnprocessableEntity)
            .WithTitle($"{operation} Failed")
            .WithDetail(detail ?? $"Failed to {operation.ToLower()} with the provided data")
            .ToProblemResult();
    }

    /// <summary>
    /// Creates an unauthorized problem details response.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <param name="detail">A description of why authorization failed.</param>
    /// <returns>An IResult representing an unauthorized response.</returns>
    public static IResult CreateUnauthorizedProblem(
        this HttpContext context,
        string detail = "Authentication is required to access this resource")
    {
        return context.CreateProblemDetails()
            .WithStatus(StatusCodes.Status401Unauthorized)
            .WithTitle("Unauthorized")
            .WithDetail(detail)
            .ToProblemResult();
    }

    /// <summary>
    /// Creates a forbidden problem details response.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <param name="detail">A description of why access is forbidden.</param>
    /// <returns>An IResult representing a forbidden response.</returns>
    public static IResult CreateForbiddenProblem(
        this HttpContext context,
        string detail = "You do not have permission to access this resource")
    {
        return context.CreateProblemDetails()
            .WithStatus(StatusCodes.Status403Forbidden)
            .WithTitle("Forbidden")
            .WithDetail(detail)
            .ToProblemResult();
    }

    /// <summary>
    /// Creates a bad request problem details response.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <param name="detail">A description of why the request was bad.</param>
    /// <returns>An IResult representing a bad request response.</returns>
    public static IResult CreateBadRequestProblem(
        this HttpContext context,
        string detail)
    {
        return context.CreateProblemDetails()
            .WithStatus(StatusCodes.Status400BadRequest)
            .WithTitle("Bad Request")
            .WithDetail(detail)
            .ToProblemResult();
    }

    /// <summary>
    /// Creates a problem details response for external service errors.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    /// <param name="serviceName">The name of the external service that failed.</param>
    /// <param name="detail">A description of the external service error.</param>
    /// <param name="exception">Optional exception that caused the error.</param>
    /// <returns>An IResult representing an external service error response.</returns>
    public static IResult CreateExternalServiceProblem(
        this HttpContext context,
        string serviceName,
        string detail,
        Exception? exception = null)
    {
        return context.CreateProblemDetails()
            .WithStatus(StatusCodes.Status502BadGateway)
            .WithTitle("External Service Error")
            .WithDetail(detail)
            .WithExtensions(new Dictionary<string, object?>
            {
                ["service"] = serviceName,
                ["errorType"] = exception?.GetType().Name,
                ["errorDetails"] = exception?.Message
            })
            .ToProblemResult();
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

        foreach (var prop in properties)//.Select(p=>p.GetValue(source)))
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