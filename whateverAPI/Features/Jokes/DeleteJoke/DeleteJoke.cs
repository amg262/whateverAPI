using FastEndpoints;
using FluentValidation;
using whateverAPI.Services;

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

public class DeleteJoke : Endpoint<Request>
{
    private readonly IJokeService _jokeService;

    public DeleteJoke(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }

    public override void Configure()
    {
        Delete("/jokes/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Delete a joke";
            s.Description = "Deletes a joke by its ID";
            s.Response(204, "Joke deleted successfully");
            s.Response(404, "Joke not found");
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var jokeDeleted = await _jokeService.DeleteJoke(req.Id);

        if (!jokeDeleted)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendNoContentAsync(ct);
    }
}