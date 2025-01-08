using FluentValidation;

namespace whateverAPI.Helpers;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator) => _validator = validator;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (context.Arguments.FirstOrDefault(x => x?.GetType() == typeof(T)) is not T argument)
            return TypedResults.BadRequest("Invalid request");

        var validationResult = await _validator.ValidateAsync(argument);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        return await next(context);
    }
}