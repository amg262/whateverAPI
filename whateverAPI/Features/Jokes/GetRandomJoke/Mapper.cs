using FastEndpoints;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.GetRandomJoke;

public class Mapper : IMapper, IResponseMapper
{
    public static Response FromEntity(Joke e) => new()
    {
        Id = e.Id,
        Content = e.Content,
        Type = e.Type,
        Tags = e.Tags?.Select(t=>t.Name).ToList(),
        CreatedAt = e.CreatedAt,
        LaughScore = e.LaughScore
    };
}