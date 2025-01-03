using FastEndpoints;
using FluentValidation;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.SearchJokes;

public record Request
{
    public required string Query { get; init; }
}

public class Mapper : Mapper<Request, List<JokeResponse>, List<Joke>>
{
    public override List<JokeResponse> FromEntity(List<Joke> jokes) => jokes.Select(joke => new JokeResponse
    {
        Id = joke.Id,
        Content = joke.Content,
        Type = joke.Type,
        CreatedAt = joke.CreatedAt,
        Tags = joke.Tags?.Select(t => t.Name).ToList() ?? [],
        LaughScore = joke.LaughScore
    }).ToList();
}

public class Validator : Validator<Request>
{
    public Validator() => RuleFor(x => x.Query).NotEmpty().WithMessage("Query is required.");
}