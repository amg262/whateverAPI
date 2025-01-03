using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.UpdateJoke;

public class Request
{
    public Guid Id { get; init; }
    public string? Content { get; init; }
    public JokeType? Type { get; init; }
    public List<string>? Tags { get; init; } = [];
    public int? LaughScore { get; init; }
}

public class Mapper : Mapper<Request, JokeResponse, Joke>
{
    public override JokeResponse FromEntity(Joke joke) => new()
    {
        Id = joke.Id,
        Content = joke.Content,
        Type = joke.Type,
        CreatedAt = joke.CreatedAt,
        Tags = joke.Tags?.Select(t => t.Name).ToList() ?? [],
        LaughScore = joke.LaughScore
    };

    public override Joke ToEntity(Request request) => new()
    {
        Id = request.Id,
        Content = request.Content,
        Type = request.Type,
        Tags = request.Tags?.Select(t => new Tag { Name = t }).ToList() ?? [],
        LaughScore = request.LaughScore
    };
}

public class Validator : Validator<Request>
{
    public Validator()
    {
        // RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
        RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");
        // RuleFor(x => x.Type).NotEmpty().WithMessage("Type is required.");
    }
}