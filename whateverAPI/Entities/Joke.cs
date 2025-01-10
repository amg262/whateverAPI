using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using whateverAPI.Data;
using whateverAPI.Models;

namespace whateverAPI.Entities;

public class Joke : IEntity<Guid>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public required string Content { get; set; }
    public JokeType? Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public List<Tag>? Tags { get; set; } = [];
    public int? LaughScore { get; set; }
    
    public bool IsActive { get; set; } = true;


    public static List<JokeResponse> ToJokeResponses(List<Joke> jokes) => jokes.Select(joke => new JokeResponse
    {
        Id = joke.Id,
        Content = joke.Content,
        Type = joke.Type,
        CreatedAt = joke.CreatedAt,
        ModifiedAt = joke.ModifiedAt,
        Tags = joke.Tags?
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => t.Name)
            .ToList() ?? [],
        LaughScore = joke.LaughScore,
        IsActive = joke.IsActive
    }).ToList();

    // Mapping methods for this entity
    public static Joke FromCreateRequest(CreateJokeRequest request)
    {
        return new Joke
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Content = request.Content,
            Type = request.Type,
            Tags = request.Tags?.Select(tagName =>
                new Tag
                {
                    // Id = Guid.CreateVersion7(),
                    Name = tagName.ToLower().Trim()
                }).ToList() ?? [],
            LaughScore = request.LaughScore,
            IsActive = true
        };
    }

    public static Joke FromUpdateRequest(Guid id, UpdateJokeRequest request)
    {
        return new Joke
        {
            Id = id,
            Content = request.Content,
            ModifiedAt = DateTime.UtcNow,
            Type = request.Type,
            Tags = request.Tags?.Select(tagName =>
                new Tag
                {
                    // Id = Guid.CreateVersion7(),
                    Name = tagName.ToLower().Trim()
                }).ToList() ?? [],
            LaughScore = request.LaughScore,
            IsActive = request.IsActive
        };
    }

    public static Joke FromJokeApiResponse(JokeApiResponse response)
    {
        return new Joke
        {
            Id = Guid.CreateVersion7(),
            Content = response.Type.Equals("single",
                StringComparison.CurrentCultureIgnoreCase)
                ? response.Joke
                : $"{response.Setup}\n{response.Delivery}",
            Type = JokeType.ThirdParty,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Tags =
            [
                new Tag
                {
                    // Id = Guid.CreateVersion7(),
                    Name = response.Category.ToLower().Trim()
                }
            ],
            LaughScore = 0,
            IsActive = true
        };
    }

    public JokeResponse ToResponse()
    {
        return new JokeResponse
        {
            Id = Id,
            Content = Content,
            Type = Type,
            Tags = Tags?
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => t.Name)
                .ToList() ?? [],
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            LaughScore = LaughScore,
            IsActive = IsActive
        };
    }

    public static JokeResponse? ToResponse(Joke? joke)
    {
        return joke?.ToResponse();
    }
}