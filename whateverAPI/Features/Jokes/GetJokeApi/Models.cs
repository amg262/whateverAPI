using FastEndpoints;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.GetJokeApi;

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