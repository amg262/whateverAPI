using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace whateverAPI.Helpers;

/// <summary>
/// Provides standardized request validation using FluentValidation, implementing RFC 7807 Problem Details
/// for HTTP APIs to ensure consistent error reporting across the application.
/// </summary>
/// <typeparam name="T">The type of request model to validate. Must be a reference type.</typeparam>
/// <remarks>
/// This filter implements a comprehensive validation pipeline that enhances API reliability and debugging:
/// 
/// Validation Process:
/// The filter performs request validation in several stages:
/// 1. Establishes correlation tracking for end-to-end request tracing
/// 2. Locates and extracts the model to validate from the request
/// 3. Applies FluentValidation rules to the model
/// 4. Generates structured validation responses for any failures
/// 
/// Problem Details Implementation:
/// Follows RFC 7807 specifications for HTTP API problem details, providing:
/// - Consistent error response structure
/// - Machine-readable error types
/// - Human-readable error descriptions
/// - Detailed validation context
/// - Environment-specific debugging information
/// 
/// Security and Debugging Features:
/// - Correlation ID tracking for request tracing
/// - Structured logging with contextual information
/// - Environment-aware error detail exposure
/// - Standardized error response formats
/// 
/// Usage Example:
/// This filter is typically applied to API endpoints that require request validation:
/// </remarks>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ValidationFilter<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the ValidationFilter with necessary dependencies for
    /// request validation and error handling.
    /// </summary>
    /// <param name="validator">FluentValidation validator for type T</param>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context</param>
    /// <param name="environment">Provides environment information for conditional behavior</param>
    /// <param name="logger">Logger for validation events and errors</param>
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
    /// Processes incoming requests through the validation pipeline, ensuring data validity
    /// before allowing the request to proceed.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context</param>
    /// <param name="next">The delegate for the next filter in the pipeline</param>
    /// <returns>
    /// Either the result of the next filter in the pipeline if validation succeeds,
    /// or a Problem Details response if validation fails.
    /// </returns>
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

    /// <summary>
    /// Creates a detailed validation problem response that adheres to RFC 7807 specifications
    /// while providing rich debugging information.
    /// </summary>
    /// <param name="validationResult">The validation result containing any validation failures</param>
    /// <param name="httpContext">The current HTTP context for request details</param>
    /// <returns>A ValidationProblemDetails object containing structured error information</returns>
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

    /// <summary>
    /// Creates a standard problem details response for non-validation related errors.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for the response</param>
    /// <param name="title">A brief, human-readable summary of the problem</param>
    /// <param name="detail">A detailed explanation of the error</param>
    /// <param name="httpContext">The current HTTP context</param>
    /// <returns>A ProblemDetails object containing the error information</returns>
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

    private static string FormatValidationErrors(IEnumerable<FluentValidation.Results.ValidationFailure> errors) =>
        string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
}