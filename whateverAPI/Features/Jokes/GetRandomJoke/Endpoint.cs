using FastEndpoints;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetRandomJoke;

public class Endpoint : EndpointWithoutRequest<Response, Mapper>
{
    private readonly IJokeService _jokeService;

    public Endpoint(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }

    public override void Configure()
    {
        Get("/jokes/random");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a random joke";
            s.Description = "Retrieves a random joke from the collection";
            s.Response<Response>(200, "Random joke retrieved successfully");
            s.Response(404, "No jokes available");
        });
        Options(o => o.WithTags("Jokes"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var joke = await _jokeService.GetRandomJoke();
        if (joke == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var response = Mapper.FromEntity(joke);
        await SendOkAsync(response, ct);
    }
}