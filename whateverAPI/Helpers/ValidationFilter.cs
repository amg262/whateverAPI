using FluentValidation;

namespace whateverAPI.Helpers;

/// <summary>
/// A generic endpoint filter that performs FluentValidation-based validation on incoming requests.
/// This filter integrates with ASP.NET Core's minimal API endpoints to validate request models
/// before they reach the endpoint handler.
/// </summary>
/// <typeparam name="T">The type of the request model to validate. Must be a reference type.</typeparam>
/// <remarks>
/// This filter works by intercepting requests to endpoints and validating any arguments that match
/// the specified type parameter T. It uses FluentValidation's IValidator interface to perform the
/// actual validation.
/// 
/// The filter will:
/// 1. Check if the request contains an argument of type T
/// 2. If found, validate it using the provided IValidator<T/>
/// 3. Return a 400 Bad Request if the argument is missing or invalid
/// 4. Allow the request to proceed if validation passes
/// </remarks>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    /// <summary>
    /// Initializes a new instance of the ValidationFilter with the specified validator.
    /// </summary>
    /// <param name="validator">The FluentValidation validator to use for request validation.</param>
    public ValidationFilter(IValidator<T> validator) => _validator = validator;

    /// <summary>
    /// Invokes the filter to perform validation on the incoming request.
    /// </summary>
    /// <param name="context">The context containing the endpoint's arguments and metadata.</param>
    /// <param name="next">The delegate to invoke the next filter or the endpoint handler.</param>
    /// <returns>
    /// - A BadRequest result if the request doesn't contain an argument of type T
    /// - A ValidationProblem result if validation fails, containing validation error details
    /// - The result of the next filter or endpoint handler if validation passes
    /// </returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.FirstOrDefault(x => x?.GetType() == typeof(T)) is not T argument)
            return TypedResults.BadRequest("Invalid request");

        var validationResult = await _validator.ValidateAsync(argument);

        return validationResult.IsValid
            ? await next(context)
            : TypedResults.ValidationProblem(validationResult.ToDictionary());
    }
}