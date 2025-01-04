using FastEndpoints;
using FluentValidation;
using whateverAPI.Helpers;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetJoke;

public record Request
{
    public Guid Id { get; init; }
}

public class Validator : Validator<Request>
{
    public Validator() => RuleFor(x => x.Id).NotEmpty().WithMessage("Joke ID is required");
}

public class GetJoke : Endpoint<Request, JokeResponse>
{
    private readonly IJokeService _jokeService;

    public GetJoke(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }

    public override void Configure()
    {
        Get("/jokes/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a specific joke by ID";
            s.Description = "Retrieves a specific joke using its unique identifier";
            s.Response<JokeResponse>(200, "Joke retrieved successfully");
            s.Response(400, "Invalid joke ID format");
            s.Response(404, "Joke not found");
        });
        // Options(o => o.WithTags("Jokes"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // The validator will ensure the ID is valid before we get here
        var joke = await _jokeService.GetJokeById(req.Id);

        if (joke == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var response = EntityMapper.JokeToJokeResponse(joke);
        await SendOkAsync(response, ct);
    }
}