using FastEndpoints;
using whateverAPI.Helpers;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes;

public class GetJokeApi : EndpointWithoutRequest<JokeResponse>
{
    private readonly JokeApiService _jokeApiService;

    public GetJokeApi(JokeApiService jokeApiService)
    {
        _jokeApiService = jokeApiService;
    }

    public override void Configure()
    {
        Get("/jokes/whatever");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a random joke";
            s.Description = "Retrieves a random joke from the joke API";
            s.Response<JokeResponse>(200, "Joke retrieved successfully");
            s.Response(404, "No jokes found");
        });
        // Options(o => o.WithTags("Jokes"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var joke = await _jokeApiService.GetExternalJoke();
        
        if (joke == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }
        var response = EntityMapper.JokeToJokeResponse(joke);

        await SendCreatedAtAsync<GetJoke>(new { joke.Id }, response, cancellation: ct);
    }
}