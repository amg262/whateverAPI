using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using whateverAPI.Data;
using whateverAPI.Models;


namespace whateverAPI.Entities;

/// <summary>
/// Represents a tag entity in the database, implementing a lightweight categorization system
/// that can be associated with jokes through a many-to-many relationship. Tags provide a 
/// flexible way to organize and categorize content while maintaining data integrity.
/// </summary>
/// <remarks>
/// This entity serves as a fundamental building block for content categorization, implementing
/// several important patterns and features:
/// </remarks>
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
    
    
    public static Tag Create(string tagName) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = tagName.ToLower().Trim(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        IsActive = true
    };
    
    /// <summary>
    /// Converts a collection of tag entities to their response representation,
    /// implementing proper sorting and data projection.
    /// </summary>
    /// <param name="tags">Collection of tag entities to convert</param>
    /// <returns>A sorted list of tag responses ready for API consumption</returns>
    /// <remarks>
    /// This method ensures consistent data presentation by:
    /// - Applying case-insensitive sorting
    /// - Mapping to response objects
    /// - Maintaining collection order
    /// </remarks>
    public static List<TagResponse> ToTagResponses(IEnumerable<Tag> tags) =>
        tags.Select(t => t.ToResponse())
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();


    public static List<TagResponse> FromTags(IEnumerable<Tag> tags) => tags.Select(t => t.ToResponse()).ToList();

    /// <summary>
    /// Creates a new tag entity from a simple name string, implementing proper
    /// initialization and data normalization.
    /// </summary>
    /// <param name="name">The name to use for the new tag</param>
    /// <returns>A fully initialized tag entity ready for persistence</returns>
    /// <remarks>
    /// This factory method ensures consistent tag creation by:
    /// - Generating a unique GUID
    /// - Normalizing the tag name
    /// - Setting appropriate timestamps
    /// - Initializing the active status
    /// </remarks>
    public static Tag FromName(string name) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name.ToLower().Trim(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        IsActive = true
    };

    /// <summary>
    /// Converts the current tag entity to its API response representation,
    /// implementing proper data projection.
    /// </summary>
    /// <returns>A tag response object ready for API consumption</returns>
    private TagResponse ToResponse()
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

    /// <summary>
    /// Converts a tag entity to its response representation, handling null values.
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public static TagResponse? ToResponse(Tag? tag) => tag?.ToResponse();

    /// <summary>
    /// Creates a new tag entity from an API creation request, implementing
    /// proper data initialization and normalization.
    /// </summary>
    /// <param name="request">The API request containing new tag data</param>
    /// <returns>A fully initialized tag entity ready for persistence</returns>
    /// <remarks>
    /// This factory method ensures proper tag creation by:
    /// - Generating a unique identifier
    /// - Normalizing the tag name
    /// - Setting creation timestamps
    /// - Initializing activity status
    /// </remarks>
    public static Tag FromCreateRequest(CreateTagRequest request) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = request.Name.ToLower().Trim(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        IsActive = true
    };

    /// <summary>
    /// Creates a tag entity from an update request, implementing proper
    /// data update patterns.
    /// </summary>
    /// <param name="request">The request containing updated tag data</param>
    /// <returns>A tag entity with updated values</returns>
    /// <remarks>
    /// This factory method handles updates by:
    /// - Preserving immutable properties
    /// - Updating modification timestamp
    /// - Normalizing updated name
    /// - Managing activity status
    /// </remarks>
    public static Tag FromUpdateRequest(UpdateTagRequest request)
    {
        return new Tag
        {
            Name = request.Name.ToLower().Trim(),
            ModifiedAt = DateTime.UtcNow,
            IsActive = request.IsActive
        };
    }

    /// <summary>
    /// Applies updates from a request directly to the current tag instance,
    /// implementing in-place modification patterns.
    /// </summary>
    /// <param name="request">The request containing updated values</param>
    public void ApplyUpdate(UpdateTagRequest request)
    {
        Name = request.Name.ToLower().Trim();
        ModifiedAt = DateTime.UtcNow;
        IsActive = request.IsActive;
    }
}