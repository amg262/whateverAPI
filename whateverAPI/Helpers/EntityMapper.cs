﻿using whateverAPI.Entities;
using whateverAPI.Features.Jokes;
using whateverAPI.Features.Jokes.UpdateJoke;

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

    public static JokeResponse JokeToJokeResponse(Joke e) => new()
    {
        Id = e.Id,
        Content = e.Content,
        Type = e.Type,
        Tags = e.Tags?.Select(t => t.Name).ToList() ?? [],
        CreatedAt = e.CreatedAt,
        LaughScore = e.LaughScore
    };

    public static List<JokeResponse> JokesToJokeReponses(List<Joke> jokes) => jokes.Select(joke => new JokeResponse
    {
        Id = joke.Id,
        Content = joke.Content,
        Type = joke.Type,
        CreatedAt = joke.CreatedAt,
        Tags = joke.Tags?.Select(t => t.Name).ToList() ?? [],
        LaughScore = joke.LaughScore
    }).ToList();

    public static Joke RequestToJoke(Request request) => new()
    {
        Id = request.Id,
        Content = request.Content,
        Type = request.Type,
        Tags = request.Tags?.Select(t => new Tag { Name = t }).ToList() ?? [],
        LaughScore = request.LaughScore
    };
}