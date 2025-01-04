using System.Text.Json.Serialization;

namespace whateverAPI.Features.Jokes;

/// <summary>
/// Represents a response from the JokeAPI service.
/// This class maps directly to the JSON structure returned by the API.
/// </summary>
public class JokeApiResponse
{
    [JsonPropertyName("error")]
    public bool Error { get; set; }
    
    [JsonPropertyName("joke")]
    public string Joke { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("setup")]
    public string? Setup { get; set; }

    [JsonPropertyName("delivery")]
    public string? Delivery { get; set; }

    [JsonPropertyName("flags")]
    public JokeFlags Flags { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("safe")]
    public bool Safe { get; set; }

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = string.Empty;
}

/// <summary>
/// Represents the content flags associated with a joke,
/// indicating various categories of potentially sensitive content.
/// </summary>
public class JokeFlags
{
    [JsonPropertyName("nsfw")]
    public bool Nsfw { get; set; }

    [JsonPropertyName("religious")]
    public bool Religious { get; set; }

    [JsonPropertyName("political")]
    public bool Political { get; set; }

    [JsonPropertyName("racist")]
    public bool Racist { get; set; }

    [JsonPropertyName("sexist")]
    public bool Sexist { get; set; }

    [JsonPropertyName("explicit")]
    public bool Explicit { get; set; }
}

