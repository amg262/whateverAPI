using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes;

public record JokeResponse
{
    public Guid Id { get; init; }
    public string? Content { get; init; }
    public JokeType? Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<string>? Tags { get; init; } = [];
    public int? LaughScore { get; init; }
}