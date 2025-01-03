using FastEndpoints;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.UpdateJoke;

public class Endpoint : Endpoint<Request, JokeResponse, Mapper>
{
    private readonly IJokeService _jokeService;

    public Endpoint(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }

    public override void Configure()
    {
        Put("/jokes/{id}"); // RESTful convention for updates
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update a joke's content";
            s.Description = "Updates the content of an existing joke by its ID";
            s.Response<JokeResponse>(200, "Joke updated successfully");
            s.Response(400, "Invalid request");
            s.Response(404, "Joke not found");
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var joke = Map.ToEntity(req);

        var updatedJoke = await _jokeService.UpdateJoke(joke);

        if (updatedJoke == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var response = Map.FromEntity(updatedJoke);
        await SendOkAsync(response, ct);
    }
}