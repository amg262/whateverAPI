using whateverAPI.Entities;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetRandomJoke;

public class Request
{
    public JokeType Type { get; set; }
    public int? PageSize { get; set; }
    public int? PageNumber { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }
}