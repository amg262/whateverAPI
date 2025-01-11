using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using whateverAPI.Data;
using whateverAPI.Models;

namespace whateverAPI.Entities;

public class Tag : IEntity<Guid>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public required string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public bool IsActive { get; set; }

    // Navigation property for many-to-many relationship
    // [JsonIgnore] [NotMapped] public List<Joke> Jokes { get; set; } = [];

    public static List<TagResponse> ToTagResponses(IEnumerable<Tag> tags) =>
        tags.Select(t => t.ToResponse())
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();


    public static List<TagResponse> FromTags(IEnumerable<Tag> tags) => tags.Select(t => t.ToResponse()).ToList();


    public static Tag FromName(string name) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name.ToLower().Trim(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        IsActive = true
    };

    // Response mapping
    public TagResponse ToResponse()
    {
        return new TagResponse
        {
            Id = Id,
            Name = Name,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            IsActive = IsActive
        };
    }

    // Create mapping
    public static Tag FromCreateRequest(CreateTagRequest request) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = request.Name.ToLower().Trim(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        IsActive = true
    };

// Create mapping
    public static Tag FromUpdateRequest(UpdateTagRequest request)
    {
        return new Tag
        {
            Name = request.Name.ToLower().Trim(),
            ModifiedAt = DateTime.UtcNow,
            IsActive = request.IsActive
        };
    }

// Update mapping
    public void ApplyUpdate(UpdateTagRequest request)
    {
        Name = request.Name.ToLower().Trim();
        ModifiedAt = DateTime.UtcNow;
        IsActive = request.IsActive;
    }
}