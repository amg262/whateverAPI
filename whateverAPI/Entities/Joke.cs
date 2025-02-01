using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using whateverAPI.Data;
using whateverAPI.Models;

namespace whateverAPI.Entities;

/// <summary>
/// Represents a joke entity in the database, implementing a rich domain model with mapping capabilities
/// and relationship management. This entity serves as both a database model and a domain object,
/// handling data persistence and business logic for jokes in the system.
/// </summary>
/// <remarks>
/// Data Relationships:
/// - Many-to-many relationship with Tags
/// - Proper cascade behavior for related entities
/// - Efficient querying support through navigation properties
/// 
/// The entity supports various data operations through its mapping methods,
/// providing a clean interface between different application layers while
/// maintaining data consistency and domain rules.
/// </remarks>
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

    public Guid? UserId { get; set; }

    [ForeignKey(nameof(UserId))] public User? User { get; set; }

    public bool IsActive { get; set; } = true;

    public static Joke Create(string content, JokeType type, List<Tag> tags, int laughScore, Guid? userId) => new()
    {
        Id = Guid.CreateVersion7(),
        Content = content,
        Type = type,
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        Tags = tags,
        LaughScore = laughScore,
        IsActive = true,
        UserId = userId
    };

    /// <summary>
    /// Converts a collection of joke entities to their response representation,
    /// implementing a clean separation between domain and API layers.
    /// </summary>
    /// <param name="jokes">Collection of joke entities to convert</param>
    /// <returns>Collection of joke responses with properly formatted data</returns>
    /// <remarks>
    /// This method handles several important transformations:
    /// - Proper tag ordering for consistent presentation
    /// - Null safety for optional properties
    /// - Clean data projection for API responses
    /// </remarks>
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
        IsActive = joke.IsActive,
        Author = User.ToUserAuthor(joke?.User)
    }).ToList();

    /// <summary>
    /// Creates a new joke entity from an API creation request, implementing proper
    /// domain object construction with consistent data initialization.
    /// </summary>
    /// <param name="request">The API request containing new joke data</param>
    /// <param name="user"></param>
    /// <returns>A fully initialized joke entity ready for persistence</returns>
    /// <remarks>
    /// This factory method ensures:
    /// - Proper ID generation using GUIDs
    /// - Consistent timestamp initialization
    /// - Proper tag name normalization
    /// - Default value initialization
    /// - Active status setting
    /// </remarks>
    public static Joke FromCreateRequest(CreateJokeRequest request, User? user) => new()
    {
        Id = Guid.CreateVersion7(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        Content = request.Content,
        Type = request.Type,
        Tags = request.Tags?.Select(tagName =>
            // We don't want to assign any kind of timestamps of GUIDs here because the TagService will handle that
            // during joke creation
            new Tag
            {
                Name = tagName.ToLower().Trim(),
            }).ToList() ?? [],
        LaughScore = request.LaughScore,
        IsActive = true,
        User = user,
        UserId = user?.Id,
    };


    /// <summary>
    /// Creates a joke entity from an update request, maintaining existing identity
    /// while applying new values from the request.
    /// </summary>
    /// <param name="id">The existing joke's identifier</param>
    /// <param name="request">The update request containing new values</param>
    /// <returns>A joke entity with updated values ready for persistence</returns>
    /// <remarks>
    /// This method implements update logic that:
    /// - Preserves entity identity
    /// - Updates modification timestamp
    /// - Handles tag updates properly
    /// - Maintains data consistency
    /// </remarks>
    public static Joke FromUpdateRequest(Guid id, UpdateJokeRequest request) => new()
    {
        Id = id,
        Content = request.Content,
        ModifiedAt = DateTime.UtcNow,
        Type = request.Type,
        Tags = request.Tags?.Select(tagName =>
            new Tag
            {
                // We don't want to assign any kind of timestamps of GUIDs here because the TagService will handle that
                // during joke updating
                Name = tagName.ToLower().Trim()
            }).ToList() ?? [],
        LaughScore = request.LaughScore,
        IsActive = request.IsActive,
    };


    /// <summary>
    /// Creates a joke entity from an external API response, implementing proper
    /// data transformation and normalization.
    /// </summary>
    /// <param name="response">The external API response containing joke data</param>
    /// <returns>A properly formatted joke entity ready for local storage</returns>
    /// <remarks>
    /// This method handles several special cases:
    /// - Different joke format types (single vs. setup/delivery)
    /// - Category to tag conversion
    /// - Proper timestamp initialization
    /// - Default score setting
    /// </remarks>
    public static Joke FromJokeApiResponse(JokeApiResponse response) => new()
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


    /// <summary>
    /// Converts the current joke entity to its API response representation,
    /// implementing proper data projection.
    /// </summary>
    /// <returns>A formatted joke response ready for API consumption</returns>
    /// <remarks>
    /// The conversion process includes:
    /// - Tag ordering and normalization
    /// - Null handling for optional properties
    /// - Proper data formatting for API consumers
    /// - Complete entity projection
    /// </remarks>
    private JokeResponse ToResponse() => new()
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
        IsActive = IsActive,
        Author = User.ToUserAuthor(User)
    };


    /// <summary>
    /// Provides a null-safe way to convert a potentially null joke entity
    /// to its response representation.
    /// </summary>
    /// <param name="joke">The joke entity that might be null</param>
    /// <returns>A joke response if the input is not null; otherwise, null</returns>
    /// <remarks>
    /// This helper method ensures safe handling of:
    /// - Null entities
    /// - Optional conversions
    /// - Consistent null propagation
    /// </remarks>
    public static JokeResponse? ToResponse(Joke? joke) => joke?.ToResponse();

    /// <summary>
    /// Updates an existing joke's properties with values from a new joke, preserving
    /// existing values when new values are null.
    /// </summary>
    /// <param name="existingJoke">The joke entity to update</param>
    /// <param name="newJoke">The joke containing new values</param>
    public static void MapJokeUpdate(Joke existingJoke, Joke newJoke)
    {
        existingJoke.ModifiedAt = DateTime.UtcNow;
        if (newJoke.Content != null) existingJoke.Content = newJoke.Content;
        if (newJoke.Type != null) existingJoke.Type = newJoke.Type;
        if (newJoke.LaughScore != null) existingJoke.LaughScore = newJoke.LaughScore;
        existingJoke.IsActive = newJoke.IsActive;
    }

    /// <summary>
    /// Updates an existing joke's properties with values from a new joke, preserving
    /// existing values when new values are null.
    /// </summary>
    /// <param name="existingJoke">The joke entity to update</param>
    /// <param name="newJoke">The joke containing new values</param>
    public static Joke UpdateExistingJoke(Joke existingJoke, Joke newJoke)
    {
        existingJoke.ModifiedAt = DateTime.UtcNow;
        if (newJoke.Content != null) existingJoke.Content = newJoke.Content;
        if (newJoke.Type != null) existingJoke.Type = newJoke.Type;
        if (newJoke.LaughScore != null) existingJoke.LaughScore = newJoke.LaughScore;
        existingJoke.IsActive = newJoke.IsActive;
        return existingJoke;
    }
}