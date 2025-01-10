using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace whateverAPI.Helpers;

/// <summary>
/// A generic endpoint filter that performs FluentValidation-based validation on incoming requests,
/// providing rich, consistent validation responses that align with problem details specifications.
/// </summary>
/// <typeparam name="T">The type of the request model to validate. Must be a reference type.</typeparam>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ValidationFilter<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the ValidationFilter with required services.
    /// </summary>
    public ValidationFilter(
        IValidator<T> validator,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        ILogger<ValidationFilter<T>> logger)
    {
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the filter to perform validation on the incoming request.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var correlationId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        // Create a logging scope for the entire validation process
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Path"] = context.HttpContext.Request.Path,
            ["Method"] = context.HttpContext.Request.Method,
            ["ValidatedType"] = typeof(T).Name
        });

        _logger.LogInformation(
            "Starting validation for request of type {RequestType} at {Endpoint}",
            typeof(T).Name,
            context.HttpContext.Request.Path);

        // Find the argument of type T in the request
        if (context.Arguments.FirstOrDefault(x => x?.GetType() == typeof(T)) is not T argument)
        {
            _logger.LogWarning(
                "Request validation failed: No valid argument of type {RequestType} found in request",
                typeof(T).Name);

            return CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid Request",
                "Request body is missing or malformed",
                context.HttpContext);
        }

        // Perform validation
        _logger.LogDebug("Executing validation rules for {RequestType}", typeof(T).Name);

        var validationResult = await _validator.ValidateAsync(argument);

        if (validationResult.IsValid)
        {
            _logger.LogInformation(
                "Validation successful for {RequestType}",
                typeof(T).Name);

            return await next(context);
        }

        // Log validation failures with detailed information
        _logger.LogWarning(
            "Validation failed for {RequestType} with {ErrorCount} errors: {Errors}",
            typeof(T).Name,
            validationResult.Errors.Count,
            FormatValidationErrors(validationResult.Errors));

        // Create a structured validation response
        var problemDetails = CreateValidationProblemDetails(
            validationResult,
            context.HttpContext);

        // Add correlation ID header for tracking
        context.HttpContext.Response.Headers.Append("X-Correlation-ID", correlationId);

        return Results.Json(
            problemDetails,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            contentType: "application/problem+json");
    }

    private ValidationProblemDetails CreateValidationProblemDetails(
        FluentValidation.Results.ValidationResult validationResult,
        HttpContext httpContext)
    {
        var problemDetails = new ValidationProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path,
            Type = "https://httpstatuses.com/422"
        };

        // Group validation errors by property
        foreach (var error in validationResult.Errors)
        {
            if (!problemDetails.Errors.TryGetValue(error.PropertyName, out var value))
            {
                problemDetails.Errors[error.PropertyName] = [error.ErrorMessage];
            }
            else
            {
                var errors = value.ToList();
                errors.Add(error.ErrorMessage);
                problemDetails.Errors[error.PropertyName] = errors.ToArray();
            }
        }

        // Add additional context to extensions
        var extensions = new Dictionary<string, object>
        {
            ["correlationId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier,
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["requestMethod"] = httpContext.Request.Method
        };

        // Include additional details in development
        if (_environment.IsDevelopment())
        {
            extensions["validatedType"] = typeof(T).Name;
            extensions["errorCount"] = validationResult.Errors.Count;
        }

        foreach (var extension in extensions)
        {
            problemDetails.Extensions[extension.Key] = extension.Value;
        }

        return problemDetails;
    }

    private static ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        HttpContext httpContext)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}",
            Extensions =
            {
                ["correlationId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier,
                ["timestamp"] = DateTimeOffset.UtcNow,
                ["requestMethod"] = httpContext.Request.Method
            }
        };
    }

    private static string FormatValidationErrors(IEnumerable<FluentValidation.Results.ValidationFailure> errors)
    {
        // Create a concise summary of validation errors for logging
        return string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
    }
}