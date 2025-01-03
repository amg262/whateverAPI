using FastEndpoints;
using whateverAPI.Features.Jokes.SearchJokes;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetJokes;

public class Endpoint : EndpointWithoutRequest<List<JokeResponse>, Mapper>
{
    private readonly IJokeService _jokeService;

    public Endpoint(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }
    
    public override void Configure()
    {
        Get("/jokes");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get jokes";
            s.Description = "Get all jokes";
            s.Response<List<JokeResponse>>(200, "Jokes found successfully");
        });
    }
    
    public override async Task HandleAsync(CancellationToken ct)
    {
        var jokes = await _jokeService.GetJokes();
        var response = Map.FromEntity(jokes);
        
        await SendAsync(response, cancellation: ct);
    }
}