using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes.CreateJoke;

public class Request
{
    public string Content { get; set; } = string.Empty;
    public JokeType Type { get; set; }
    public List<string>? Tags { get; set; } = [];
    public int? LaughScore { get; set; }
}