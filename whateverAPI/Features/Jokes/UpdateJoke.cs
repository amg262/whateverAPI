using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes;



public class UpdateJoke : Endpoint<UpdateJoke.Request, JokeResponse>
{
    private readonly IJokeService _jokeService;

    public UpdateJoke(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }
    
    public class Request
    {
        public Guid Id { get; init; }
        public string? Content { get; init; }
        public JokeType? Type { get; init; }
        public List<string>? Tags { get; init; } = [];
        public int? LaughScore { get; init; }
    }

    public class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
            RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");
        }
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
        var joke = EntityMapper.UpdateRequestToJoke(req);
        var updatedJoke = await _jokeService.UpdateJoke(joke);

        if (updatedJoke == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var response = EntityMapper.JokeToJokeResponse(joke);
        await SendOkAsync(response, ct);
    }
}