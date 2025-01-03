using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.GetRandomJoke;

public record Request
{
    public JokeType Type { get; init; }
    public int? PageSize { get; init; }
    public int? PageNumber { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
}

public class Mapper : Mapper<EndpointWithoutRequest, JokeResponse, Joke>, IResponseMapper
{
    public override JokeResponse FromEntity(Joke e) => new()
    {
        Id = e.Id,
        Content = e.Content,
        Type = e.Type,
        Tags = e.Tags?.Select(t => t.Name).ToList() ?? [],
        CreatedAt = e.CreatedAt,
        LaughScore = e.LaughScore
    };
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