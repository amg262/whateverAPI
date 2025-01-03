using FastEndpoints;
using FluentValidation;
using whateverAPI.Data;

namespace whateverAPI.Features.Jokes.DeleteJoke;

public record Request
{
    public Guid Id { get; init; }
}

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithMessage("The ID cannot be empty.")
            .Must(id => id != Guid.Empty)
            .WithMessage("The ID must be a valid GUID.");
    }
}