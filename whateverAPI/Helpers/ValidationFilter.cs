using System.Diagnostics;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
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
/// Usage Example:
/// This filter is typically applied to API endpoints that require request validation:
/// </remarks>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;
    private readonly ProblemDetailsConfig _problemDetailsConfig;
    private readonly ILogger<ValidationFilter<T>> _logger;

    /// <summary>
    /// Initializes a new validation filter with required dependencies.
    /// </summary>
    /// <param name="validator">The FluentValidation validator for type T.</param>
    /// <param name="problemDetailsConfig">Configuration for creating standardized error responses.</param>
    /// <param name="logger">Logger for validation events and errors.</param>
    public ValidationFilter(
        IValidator<T> validator,
        ProblemDetailsConfig problemDetailsConfig,
        ILogger<ValidationFilter<T>> logger)
    {
        _validator = validator;
        _problemDetailsConfig = problemDetailsConfig;
        _logger = logger;
    }

    /// <summary>
    /// Validates requests against defined validation rules before allowing them to proceed.
    /// </summary>
    /// <param name="context">The endpoint filter context containing request data.</param>
    /// <param name="next">The delegate for the next filter in the pipeline.</param>
    /// <returns>Either the next filter's result or a problem details response if validation fails.</returns>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Set up logging context
        var correlationId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ValidatedType"] = typeof(T).Name
        });

        // Find and validate the model
        if (context.Arguments.FirstOrDefault(x => x?.GetType() == typeof(T)) is not T argument)
        {
            _logger.LogWarning("Request validation failed: No valid argument of type {Type} found", typeof(T).Name);
            return CreateInvalidRequestResponse(context.HttpContext);
        }

        // Perform validation
        var validationResult = await _validator.ValidateAsync(argument);
        if (validationResult.IsValid)
        {
            return await next(context);
        }

        // Log validation failures
        _logger.LogWarning(
            "Validation failed for {Type} with {ErrorCount} errors",
            typeof(T).Name,
            validationResult.Errors.Count);

        return CreateValidationFailureResponse(validationResult, context.HttpContext);
    }

    /// <summary>
    /// Creates a problem details response for invalid request format or missing data.
    /// </summary>
    private ProblemHttpResult CreateInvalidRequestResponse(HttpContext context)
    {
        var problem = _problemDetailsConfig.CreateProblemDetails(
            context,
            StatusCodes.Status400BadRequest,
            "Invalid Request",
            $"Request body of type {typeof(T).Name} is missing or malformed",
            new Dictionary<string, object?>
            {
                ["expectedType"] = typeof(T).Name
            });

        return TypedResults.Problem(problem);
    }

    /// <summary>
    /// Creates a detailed validation problem response containing all validation errors.
    /// </summary>
    private ProblemHttpResult CreateValidationFailureResponse(
        ValidationResult validationResult,
        HttpContext context)
    {
        // Group validation errors by property
        var errorsByProperty = validationResult.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Type = "https://httpstatuses.com/422",
            Errors = errorsByProperty
        };

        // Add validation context
        var additionalContext = new Dictionary<string, object?>
        {
            ["validatedType"] = typeof(T).Name,
            ["errorCount"] = validationResult.Errors.Count
        };

        _problemDetailsConfig.EnrichWithContext(problem, context, additionalContext);

        return TypedResults.Problem(problem);
    }
}