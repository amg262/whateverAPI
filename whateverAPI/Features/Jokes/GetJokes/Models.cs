using FastEndpoints;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.GetJokes;

public class Mapper : Mapper<EndpointWithoutRequest, List<JokeResponse>, List<Joke>>, IResponseMapper
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