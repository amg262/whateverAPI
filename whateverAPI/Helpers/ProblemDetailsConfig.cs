using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace whateverAPI.Helpers;

/// <summary>
/// Provides centralized configuration and enrichment for ProblemDetails responses,
/// ensuring consistent error response formatting and contextual information across
/// the API. Implements RFC 7807 Problem Details specification with environment-aware
/// debugging information and request tracing support.
/// </summary>
/// <param name="environment">The hosting environment used to determine appropriate error detail levels.</param>
public class ProblemDetailsConfig(IHostEnvironment environment)
{
    /// <summary>
    /// Enriches a ProblemDetails instance with standardized context information including
    /// request tracking, timestamps, and environment-specific debugging details.
    /// </summary>
    /// <param name="problem">The ProblemDetails instance to enrich.</param>
    /// <param name="context">The HttpContext of the current request.</param>
    /// <param name="additionalContext">Optional dictionary of additional context to include.</param>
    /// <returns>The enriched ProblemDetails instance.</returns>
    public ProblemDetails EnrichWithContext(
        ProblemDetails problem,
        HttpContext context,
        IDictionary<string, object?>? additionalContext = null)
    {
        // Get or generate correlation ID for request tracking
        var correlationId = Activity.Current?.Id ?? context.TraceIdentifier;
        var activity = context.Features.Get<IHttpActivityFeature>()?.Activity;

        // Build standard context information that should be included in every response
        var extensions = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["requestMethod"] = context.Request.Method,
            ["path"] = context.Request.Path,
            ["traceId"] = activity?.Id ?? correlationId
        };

        // Add development-specific information when in development environment
        if (environment.IsDevelopment())
        {
            extensions["environmentName"] = environment.EnvironmentName;
            // extensions["activityId"] = Activity.Current?.Id;
            extensions["activityParentId"] = Activity.Current?.ParentId;
        }

        // Merge any additional context provided
        if (additionalContext != null)
        {
            foreach (var (key, value) in additionalContext)
            {
                extensions[key] = value;
            }
        }

        // Apply all extensions to the problem details
        foreach (var (key, value) in extensions)
        {
            problem.Extensions[key] = value;
        }

        return problem;
    }

    /// <summary>
    /// Creates and enriches a new ProblemDetails instance with specified error details
    /// and standard context information.
    /// </summary>
    /// <param name="context">The HttpContext of the current request.</param>
    /// <param name="statusCode">The HTTP status code for the error.</param>
    /// <param name="title">A short, human-readable summary of the problem.</param>
    /// <param name="detail">A detailed explanation of the error.</param>
    /// <param name="additionalContext">Optional additional context to include.</param>
    /// <returns>A fully configured ProblemDetails instance.</returns>
    public ProblemDetails CreateProblemDetails(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        IDictionary<string, object?>? additionalContext = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };

        return EnrichWithContext(problem, context, additionalContext);
    }
}