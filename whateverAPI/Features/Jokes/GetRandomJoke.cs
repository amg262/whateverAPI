using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes;

public class GetRandomJoke : EndpointWithoutRequest<JokeResponse>
{
    private readonly IJokeService _jokeService;

    public GetRandomJoke(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }

    public record Request
    {
        public JokeType Type { get; init; }
        public int? PageSize { get; init; }
        public int? PageNumber { get; init; }
        public string? SortBy { get; init; }
        public bool? SortDescending { get; init; }
    }

    public class Validator : Validator<Request>
    {
        // private static readonly string[] _availableTypes = { "Joke", "FunnySaying", "Discouragement", "SelfDeprecating" };
        private static readonly string[] AvailableSortFields = ["createdAt", "laughScore", "content"];

        public Validator()
        {
            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid joke type. Available types are: Joke, FunnySaying, Discouragement, SelfDeprecating");

            When(x => x.PageSize.HasValue, () =>
            {
                RuleFor(x => x.PageSize!.Value)
                    .InclusiveBetween(1, 100)
                    .WithMessage("Page size must be between 1 and 100");
            });

            When(x => x.PageNumber.HasValue, () =>
            {
                RuleFor(x => x.PageNumber!.Value)
                    .GreaterThan(0)
                    .WithMessage("Page number must be greater than 0");

                RuleFor(x => x.PageSize)
                    .NotNull()
                    .WithMessage("Page size is required when using pagination");
            });

            When(x => !string.IsNullOrEmpty(x.SortBy), () =>
            {
                RuleFor(x => x.SortBy)
                    .Must(sortBy => AvailableSortFields.Contains(sortBy?.ToLower()))
                    .WithMessage($"Sort by field must be one of: {AvailableSortFields}");

                RuleFor(x => x.SortDescending)
                    .NotNull()
                    .WithMessage("Sort direction is required when using sorting");
            });
        }
    }

    public override void Configure()
    {
        Get("/jokes/random");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a random joke";
            s.Description = "Retrieves a random joke from the collection";
            s.Response<JokeResponse>(200, "Random joke retrieved successfully");
            s.Response(404, "No jokes available");
        });
        // Options(o => o.WithTags("Jokes"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var joke = await _jokeService.GetRandomJoke();
        if (joke == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var response = EntityMapper.JokeToJokeResponse(joke);
        await SendOkAsync(response, ct);
    }
}