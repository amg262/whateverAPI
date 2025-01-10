using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace whateverAPI.Helpers;

public static class ProblemDetailsHelper
{
    private static ProblemHttpResult CreateProblemDetails(
        HttpContext context,
        int statusCode,
        string title,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = instance ?? context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        var standardExtensions = new Dictionary<string, object?>
        {
            ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["requestMethod"] = context.Request.Method,
            ["endpoint"] = context.Request.Path
        };

        foreach (var extension in standardExtensions)
        {
            problemDetails.Extensions[extension.Key] = extension.Value;
        }

        if (extensions == null) return TypedResults.Problem(problemDetails);
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        return TypedResults.Problem(problemDetails);
    }

    // Authentication & Authorization
    public static IResult CreateUnauthorizedProblem(
        HttpContext context,
        string detail = "Authentication is required to access this resource")
    {
        return CreateProblemDetails(
            context,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            detail);
    }

    public static IResult CreateForbiddenProblem(
        HttpContext context,
        string detail = "You do not have permission to access this resource")
    {
        return CreateProblemDetails(
            context,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            detail);
    }

    // Resource Related
    public static IResult CreateNotFoundProblem(
        HttpContext context,
        string resourceType,
        string identifier)
    {
        return CreateProblemDetails(
            context,
            StatusCodes.Status404NotFound,
            "Resource Not Found",
            $"{resourceType} with identifier '{identifier}' was not found",
            extensions: new Dictionary<string, object?>
            {
                ["resourceType"] = resourceType,
                ["identifier"] = identifier
            });
    }

    public static IResult CreateBadRequestProblem(
        HttpContext context,
        string detail)
    {
        return CreateProblemDetails(
            context,
            StatusCodes.Status400BadRequest,
            "Bad Request",
            detail);
    }

    public static IResult CreateConflictProblem(
        HttpContext context,
        string detail)
    {
        return CreateProblemDetails(
            context,
            StatusCodes.Status409Conflict,
            "Conflict",
            detail);
    }

    // External Service Related
    public static IResult CreateExternalServiceProblem(
        HttpContext context,
        string serviceName,
        string detail,
        Exception? exception = null)
    {
        return CreateProblemDetails(
            context,
            StatusCodes.Status502BadGateway,
            "External Service Error",
            detail,
            extensions: new Dictionary<string, object?>
            {
                ["service"] = serviceName,
                ["errorType"] = exception?.GetType().Name,
                ["errorDetails"] = exception?.Message
            });
    }

    public static IResult CreateServiceUnavailableProblem(
        HttpContext context,
        string detail,
        TimeSpan? retryAfter = null)
    {
        var extensions = new Dictionary<string, object?>();
        if (retryAfter.HasValue)
        {
            extensions["retryAfter"] = retryAfter.Value.TotalSeconds;
        }

        return CreateProblemDetails(
            context,
            StatusCodes.Status503ServiceUnavailable,
            "Service Unavailable",
            detail,
            extensions: extensions);
    }

    // Operation Related
    public static IResult CreateTooManyRequestsProblem(
        HttpContext context,
        TimeSpan retryAfter)
    {
        return CreateProblemDetails(
            context,
            StatusCodes.Status429TooManyRequests,
            "Too Many Requests",
            "Rate limit exceeded. Please try again later.",
            extensions: new Dictionary<string, object?>
            {
                ["retryAfter"] = retryAfter.TotalSeconds
            });
    }

    public static IResult CreateTimeoutProblem(
        HttpContext context,
        string operation,
        TimeSpan? timeout = null)
    {
        var extensions = new Dictionary<string, object?>
        {
            ["operation"] = operation
        };

        if (timeout.HasValue)
        {
            extensions["timeout"] = timeout.Value.TotalSeconds;
        }

        return CreateProblemDetails(
            context,
            StatusCodes.Status408RequestTimeout,
            "Request Timeout",
            $"The {operation} operation timed out",
            extensions: extensions);
    }

    // Validation Related
    public static IResult CreateUnprocessableEntityProblem(
        HttpContext context,
        string detail,
        IDictionary<string, string[]>? validationErrors = null)
    {
        var extensions = new Dictionary<string, object?>();
        if (validationErrors != null)
        {
            extensions["validationErrors"] = validationErrors;
        }

        return CreateProblemDetails(
            context,
            StatusCodes.Status422UnprocessableEntity,
            "Validation Failed",
            detail,
            extensions: extensions);
    }
}