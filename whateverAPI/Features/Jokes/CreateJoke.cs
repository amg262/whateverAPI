using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes;

public class CreateJoke : Endpoint<CreateJokeRequest, JokeResponse>
{
    private readonly IJokeService _jokeService;

    public CreateJoke(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }

    public record Request
    {
        public required string Content { get; init; } = string.Empty;
        public JokeType Type { get; init; }
        public List<string>? Tags { get; init; } = [];
        public int? LaughScore { get; init; }
    }

    public class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required")
                .MinimumLength(5).WithMessage("Content must be at least 10 characters");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid joke type");

            RuleFor(x => x.Tags)
                .Must(tags => tags?.Count <= 10)
                .WithMessage("Maximum 5 tags allowed")
                .When(x => x.Tags != null);

            RuleForEach(x => x.Tags)
                .MaximumLength(20)
                .WithMessage("Tag length cannot exceed 20 characters")
                .When(x => x.Tags != null);
        }
    }

    public override void Configure()
    {
        Post("/jokes/create");
        // AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new joke";
            s.Description = "Creates a new joke entry with content, type, and optional tags";
            s.Response<JokeResponse>(201, "Joke created successfully");
            s.Response(400, "Invalid request");
        });
    }

    public override async Task HandleAsync(CreateJokeRequest req, CancellationToken ct)
    {
        var jokeEntity = EntityMapper.CreateRequestToJoke(req);
        var joke = await _jokeService.CreateJoke(jokeEntity);
        var response = EntityMapper.JokeToJokeResponse(joke);

        await SendCreatedAtAsync<GetJoke>(
            new { id = joke.Id },
            response,
            cancellation: ct
        );
    }
}