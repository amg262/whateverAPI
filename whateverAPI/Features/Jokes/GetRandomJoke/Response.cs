using whateverAPI.Entities;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetRandomJoke;

public class Response
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public JokeType? Type { get; set; }
    public List<string>? Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public int? LaughScore { get; set; }
}