using FastEndpoints;
using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.CreateJoke;

public class Mapper : Mapper<Request, Response, Joke>
{
    public override Joke ToEntity(Request r) => new()
    {
        Id = Guid.CreateVersion7(),
        Content = r.Content,
        Type = r.Type,
        CreatedAt = DateTime.UtcNow,
        LaughScore = r.LaughScore ?? 0,
        Tags = r.Tags?.Select(tagName => new Tag { Name = tagName }).ToList() ?? []
    };

    public override Response FromEntity(Joke j) => new()
    {
        Id = j.Id,
        Content = j.Content,
        Type = j.Type,
        Tags = j.Tags?.Select(t => t.Name).ToList() ?? [],
        CreatedAt = j.CreatedAt,
        LaughScore = j.LaughScore
    };
}