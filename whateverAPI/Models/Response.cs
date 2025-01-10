using System.Text.Json.Serialization;
using whateverAPI.Entities;

namespace whateverAPI.Models;

public class Response
{
}

public record JokeResponse
{
    public Guid Id { get; init; }
    public string? Content { get; init; }
    public JokeType? Type { get; init; }
    public DateTime CreatedAt { get; init; }
    
    public DateTime ModifiedAt { get; init; }
    public List<string>? Tags { get; init; } = [];
    public int? LaughScore { get; init; }
    
    // public static List<JokeResponse> FromJokes(IEnumerable<Joke> jokes)
    // {
    //     return jokes.Select(joke => joke.ToResponse()).ToList();
    // }
    //
    // public static JokeResponse? FromJoke(Joke? joke)
    // {
    //     return joke?.ToResponse();
    // }
    

}

public record TagResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }

    public static List<TagResponse> FromTags(IEnumerable<Tag> tags)
    {
        return tags.Select(t => t.ToResponse()).ToList();
    }

}

public record JokeApiResponse
{
    [JsonPropertyName("error")] public bool Error { get; set; }

    [JsonPropertyName("joke")] public string Joke { get; set; }

    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;

    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("setup")] public string? Setup { get; set; }

    [JsonPropertyName("delivery")] public string? Delivery { get; set; }

    [JsonPropertyName("flags")] public JokeFlags Flags { get; set; } = new();

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("safe")] public bool Safe { get; set; }

    [JsonPropertyName("lang")] public string Lang { get; set; } = string.Empty;
}

/// <summary>
/// Represents the content flags associated with a joke,
/// indicating various categories of potentially sensitive content.
/// </summary>
public class JokeFlags
{
    [JsonPropertyName("nsfw")] public bool Nsfw { get; set; }

    [JsonPropertyName("religious")] public bool Religious { get; set; }

    [JsonPropertyName("political")] public bool Political { get; set; }

    [JsonPropertyName("racist")] public bool Racist { get; set; }

    [JsonPropertyName("sexist")] public bool Sexist { get; set; }

    [JsonPropertyName("explicit")] public bool Explicit { get; set; }
}