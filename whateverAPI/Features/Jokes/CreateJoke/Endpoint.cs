using FastEndpoints;
using whateverAPI.Features.Jokes.GetJoke;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.CreateJoke;

public class Endpoint: Endpoint<Request, JokeResponse, Mapper>
{
    private readonly IJokeService _jokeService;
    
    public Endpoint(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }
    
    public override void Configure()
    {
        Post("/jokes/create");
        Summary(s =>
        {
            s.Summary = "Create a new joke";
            s.Description = "Creates a new joke entry with content, type, and optional tags";
            s.Response<JokeResponse>(201, "Joke created successfully");
            s.Response(400, "Invalid request");
        });
    }
    
    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var jokeEntity = Map.ToEntity(req);
        var joke = await _jokeService.CreateJoke(jokeEntity);
        JokeResponse response = Map.FromEntity(joke);

        
        await SendCreatedAtAsync<GetJoke.Endpoint>(
            new { id = joke.Id },
            response,
            cancellation: ct
        );
    }
}