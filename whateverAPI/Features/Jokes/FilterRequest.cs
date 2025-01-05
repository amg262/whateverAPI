using whateverAPI.Entities;

namespace whateverAPI.Features.Jokes;

public interface IFilterRequest
{
    public JokeType Type { get; init; }
    public int? PageSize { get; init; }
    public int? PageNumber { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
    
    public record FilterRequest : IFilterRequest
    {
        public JokeType Type { get; init; }
        public int? PageSize { get; init; }
        public int? PageNumber { get; init; }
        public string? SortBy { get; init; }
        public bool? SortDescending { get; init; }
    }
}

public record FilterRequest : IFilterRequest
{
    public JokeType Type { get; init; }
    public int? PageSize { get; init; }
    public int? PageNumber { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDescending { get; init; }
}