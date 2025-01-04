using FastEndpoints;
using whateverAPI.Helpers;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetJokesByType;

public class Endpoint : Endpoint<Request, List<JokeResponse>, Mapper>
{
    private readonly IJokeService _jokeService;
    
    public Endpoint(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }
    
    public override void Configure()
    {
        Get("/jokes/type/{Type}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get jokes by type";
            s.Description = "Retrieves all jokes of a specific type with optional pagination and sorting";
            s.Response<List<JokeResponse>>(200, "Jokes retrieved successfully");
            s.Response(400, "Invalid request parameters");
            s.Response(404, "No jokes found for the specified type");
        });
        // Options(o => o.WithTags("Jokes"));
    }
    
    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var jokes = await _jokeService.GetJokesByType(
            req.Type,
            req.PageSize,
            req.PageNumber,
            req.SortBy,
            req.SortDescending ?? false);

        if (jokes.Count == 0)
        {
            await SendNotFoundAsync(ct);
            return;
        }
        
        var response = jokes.Select(EntityMapper.JokeToJokeResponse).ToList();
        await SendOkAsync(response, ct);
    }
}