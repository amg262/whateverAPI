using FastEndpoints;
using FluentValidation;
using whateverAPI.Helpers;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes;

public class SearchJokes : Endpoint<SearchJokes.Request, List<JokeResponse>>
{
    private readonly IJokeService _jokeService;

    public SearchJokes(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }

    public record Request
    {
        public required string Query { get; init; }
    }

    public class Validator : Validator<Request>
    {
        public Validator() => RuleFor(x => x.Query).NotEmpty().WithMessage("Query is required.");
    }

    public override void Configure()
    {
        Get("/jokes/search");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Search for jokes";
            s.Description = "Search for jokes by content, type, or tags";
            // s.Query(q => q.Content, "Search by joke content");
            // s.Query(q => q.Type, "Search by joke type");
            // s.Query(q => q.Tags, "Search by joke tags");
            s.Response<List<JokeResponse>>(200, "Jokes found successfully");
            s.Response(400, "Invalid request");
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var jokes = await _jokeService.SearchJokes(req.Query);

        if (jokes == null || jokes.Count == 0)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var response = EntityMapper.JokesToJokeReponses(jokes);
        await SendAsync(response, cancellation: ct);
    }
}