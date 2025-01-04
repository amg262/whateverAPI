using FastEndpoints;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetJoke;

public class Endpoint : Endpoint<Request, JokeResponse, Mapper>
{
    private readonly IJokeService _jokeService;
    
    public Endpoint(IJokeService jokeService)
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
        
        var response = Map.FromEntity(joke);
        await SendOkAsync(response, ct);
    }
}