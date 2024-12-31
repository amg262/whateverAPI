using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.CreateJoke;

public class Response
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public JokeType? Type { get; set; }
    public List<string>? Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public int? LaughScore { get; set; }
}