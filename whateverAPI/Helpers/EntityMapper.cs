using whateverAPI.Entities;
using whateverAPI.Features.Jokes;

namespace whateverAPI.Helpers;

public static class EntityMapper
{
    public static Joke JokeApiResponseToJoke(JokeApiResponse response)
    {
        return new Joke
        {
            Id = Guid.CreateVersion7(),
            Content = response.Type.Equals("single"
                , StringComparison.CurrentCultureIgnoreCase)
                ? response.Joke
                : $"{response.Setup}\n{response.Delivery}",
            Type = JokeType.ThirdParty,
            CreatedAt = DateTime.UtcNow,
            Tags = [new Tag { Id = Guid.CreateVersion7(), Name = response.Category }],
            LaughScore = 0
        };
    }
}