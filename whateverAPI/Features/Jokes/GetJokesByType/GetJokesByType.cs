using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetJokesByType;

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
                .Must(sortBy => new[] { "createdAt", "laughScore", "content" }.Contains(sortBy?.ToLower()))
                .WithMessage("Sort by field must be one of: createdAt, laughScore, content");

            RuleFor(x => x.SortDescending)
                .NotNull()
                .WithMessage("Sort direction is required when using sorting");
        });
    }
}

public class GetJokesByType : Endpoint<Request, List<JokeResponse>>
{
    private readonly IJokeService _jokeService;

    public GetJokesByType(IJokeService jokeService)
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