using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.GetJoke;

public record Request
{
    public Guid Id { get; init; }
}


public class Validator : Validator<Request>
{
    public Validator()
    {
        // Validate that the ID is not empty
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Joke ID is required");

        // Optionally, validate that the joke exists
        // Note: This adds a database call to validation, which might not be desirable
        // You might want to remove this and handle non-existent jokes in the endpoint instead
        /*
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) => await _jokeService.JokeExistsById(id))
            .WithMessage("Joke with the specified ID does not exist");
        */
    }
}