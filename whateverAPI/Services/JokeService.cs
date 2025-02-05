using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;

namespace whateverAPI.Services;

/// <summary>
/// Defines the contract for joke management operations, providing a comprehensive set of methods
/// for creating, reading, updating, and deleting jokes in the system.
/// </summary>
/// <remarks>
/// This interface establishes the core functionality needed for joke management, including:
/// 
/// Basic CRUD Operations:
/// - Creating new jokes with duplicate detection
/// - Reading jokes through various query methods
/// - Updating existing jokes with tag management
/// - Deleting jokes from the system
/// 
/// Advanced Query Capabilities:
/// - Random joke selection
/// - Type-based filtering
/// - Full-text search
/// - Pagination and sorting
/// </remarks>
public interface IJokeService
{
    // Create operations
    Task<Joke> CreateJoke(Joke joke, CancellationToken ct = default);

    // Read operations
    Task<Joke?> GetRandomJoke(CancellationToken ct = default);
    Task<List<Joke>?> SearchJokes(string query, CancellationToken ct = default);

    Task<List<Joke>> GetJokesByType(
        JokeType type,
        int? pageSize = null,
        int? pageNumber = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken ct = default);

    Task<List<Joke>> GetJokesByType(FilterRequest request, CancellationToken ct = default);

    Task<bool> JokeExistsById(Guid id, CancellationToken ct = default);
    Task<int> GetJokesCountByType(JokeType type, CancellationToken ct = default);
    Task<Joke?> GetJokeById(Guid id, CancellationToken ct = default);
    Task<bool> DeleteJoke(Guid id, CancellationToken ct = default);
    Task<List<Joke>?> GetJokes(CancellationToken ct = default);
    Task<Joke?> UpdateJoke(Joke? joke, CancellationToken ct = default);
}

/// <summary>
/// Provides comprehensive joke management functionality with support for advanced querying,
/// filtering, and tag management.
/// </summary>
/// <remarks>
/// This service implements a robust joke management system with several key features:
/// 
/// Data Management:
/// - Duplicate detection to prevent content repetition
/// - Tag management with automatic creation and association
/// - Soft delete support through IsActive flag
/// 
/// Query Capabilities:
/// - Random joke selection with efficient database access
/// - Type-based filtering with pagination and sorting
/// - Full-text search across joke content and tags
/// - Complex filtering with multiple criteria
/// </remarks>
public class JokeService //: IJokeService
{
    private readonly AppDbContext _db;
    private readonly TagService _tagService;
    private readonly ILogger<JokeService> _logger;

    /// <summary>
    /// Initializes a new instance of the JokeService with required dependencies.
    /// </summary>
    /// <param name="db">Database context for joke persistence</param>
    /// <param name="logger">Logger for operation tracking and debugging</param>
    /// <param name="tagService">Service for managing joke tags</param>
    public JokeService(AppDbContext db, ILogger<JokeService> logger, TagService tagService)
    {
        _db = db;
        _logger = logger;
        _tagService = tagService;
    }

    /// <summary>
    /// Creates a new joke in the system with duplicate detection and tag management.
    /// </summary>
    /// <remarks>
    /// The creation process includes several steps:
    /// 1. Checks for existing jokes with identical content (case-insensitive)
    /// 2. Processes and associates tags with the joke
    /// 3. Persists the joke with its relationships
    /// 
    /// Tag processing involves:
    /// - Creating new tags if they don't exist
    /// - Finding and associating existing tags
    /// - Maintaining proper entity relationships
    /// </remarks>
    /// <param name="joke">The joke entity to create</param>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>The created joke with its associated tags</returns>
    /// <exception cref="DuplicateNameException">Thrown when identical joke content already exists</exception>
    public async Task<Joke> CreateJoke(Joke joke, CancellationToken ct = default)
    {
        var jokeExists = await _db.Jokes
            .AnyAsync(j => j.Content
                .ToLower()
                .Trim() == joke.Content
                .ToLower()
                .Trim(), ct);

        if (jokeExists) throw new DuplicateNameException("Joke with the same content already exists");

        if (joke.Tags?.Count > 0)
            {
                var newTags = joke.Tags.ToList();
                joke.Tags.Clear();

                foreach (var tag in newTags)
                {
                var tagEntity = await _tagService.CreateOrFindTagAsync(tag.Name, ct);
                joke.Tags.Add(tagEntity);
            }
            }

            _db.Jokes.Add(joke);
        await _db.SaveChangesAsync(ct);

            return await _db.Jokes
                .Include(j => j.Tags)
            .Include(j => j.User)
            .AsNoTracking()
            .FirstAsync(j => j.Id == joke.Id, ct);
    }

    /// <summary>
    /// Retrieves a random joke from the database using an efficient selection algorithm.
    /// </summary>
    /// <remarks>
    /// The selection process works as follows:
    /// 1. Gets the total count of jokes
    /// 2. Generates a random skip count
    /// 3. Uses Skip/Take for efficient random selection
    /// </remarks>
    public async Task<Joke?> GetRandomJoke(CancellationToken ct = default)
    {
        var count = await _db.Jokes.CountAsync(ct);
        if (count == 0)
            {
                _logger.LogInformation("No jokes available for random selection");
                return null;
            }

            var random = new Random();
            var skipCount = random.Next(count);

            var joke = await _db.Jokes
                .Include(j => j.Tags)
            .Include(j => j.User)
            .AsNoTracking()
                .Skip(skipCount)
            .FirstOrDefaultAsync(ct);

            if (joke != null)
            {
                _logger.LogInformation("Retrieved random joke with ID: {JokeId}", joke.Id);
            }

            return joke;
        }


    public Task<List<Joke>> GetJokesByType(JokeType type, int? pageSize = null, int? pageNumber = null, string? sortBy = null,
        bool sortDescending = false, CancellationToken ct = default)
        {
            var query = _db.Jokes
                .Include(j => j.Tags)
            .Include(j => j.User)
            .AsNoTracking()
                .Where(j => j.Type == type);

            if (!string.IsNullOrEmpty(sortBy))
            {
                Expression<Func<Joke, object>> keySelector = sortBy.ToLower() switch
                {
                    "createdat" => j => j.CreatedAt,
                    "laughscore" => j => j.LaughScore ?? 0,
                    "content" => j => j.Content,
                    _ => j => j.CreatedAt
                };
            query = query.ApplySorting(keySelector, sortDescending);
        }

        query = query.ApplyPaging(pageNumber, pageSize);

        return query.ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves jokes by type with comprehensive filtering, sorting, and pagination support.
    /// </summary>
    /// <remarks>
    /// Supports flexible querying through:
    /// - Type-based filtering
    /// - Multiple sorting options (created date, laugh score, content)
    /// - Pagination for large result sets
    /// </remarks>
    public async Task<List<Joke>> GetJokesByType(FilterRequest request, CancellationToken ct = default)
    {
        var query = _db.Jokes
            .Include(j => j.Tags)
            .Include(j => j.User)
            .AsNoTracking()
            .Where(j => j.Type == request.Type);

        if (!string.IsNullOrEmpty(request.SortBy))
        {
            Expression<Func<Joke, object>> keySelector = request.SortBy.ToLower() switch
            {
                "createdat" => j => j.CreatedAt,
                "laughscore" => j => j.LaughScore ?? 0,
                "content" => j => j.Content,
                _ => j => j.CreatedAt
            };
            query = query.ApplySorting(keySelector, request.SortDescending);
        }

        query = query.ApplyPaging(request.PageNumber, request.PageSize);

        var result = await query.ToListAsync(ct);

        _logger.LogInformation("Retrieved {Count} jokes of type {Type}", result.Count, request.Type);

        return result;
    }

    /// <summary>
    /// Checks if a joke exists in the database by its unique identifier.
    /// </summary>
    /// <remarks>
    /// This method provides a lightweight way to verify joke existence without retrieving
    /// the entire entity. It's particularly useful for validation before performing operations
    /// that require the joke to exist. The method uses Entity Framework's AnyAsync for optimal
    /// performance, as it translates to a SQL EXISTS query.
    /// </remarks>
    /// <param name="id">The unique identifier of the joke to check</param>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>True if a joke with the specified ID exists; otherwise, false</returns>
    public async Task<bool> JokeExistsById(Guid id, CancellationToken ct = default) =>
        await _db.Jokes.AnyAsync(j => j.Id == id, ct);

    /// <summary>
    /// Retrieves the total count of jokes for a specific joke type.
    /// </summary>
    /// <param name="type">The type of jokes to count</param>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>The total number of jokes of the specified type</returns>
    public async Task<int> GetJokesCountByType(JokeType type, CancellationToken ct = default) =>
        await _db.Jokes.CountAsync(j => j.Type == type, ct);

    /// <summary>
    /// Retrieves a specific joke by its unique identifier, including its associated tags.
    /// </summary>
    /// <param name="id">The unique identifier of the joke to retrieve</param>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>The requested joke with its tags if found; otherwise, null</returns>
    public async Task<Joke?> GetJokeById(Guid id, CancellationToken ct = default)
    {
        var joke = await _db.Jokes
            .Include(j => j.Tags)
            .Include(j => j.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (joke != null) return joke;

        _logger.LogInformation("Joke with ID {JokeId} not found", id);
        return null;
    }

    /// <summary>
    /// Permanently removes a joke from the database by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the joke to delete</param>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>True if the joke was successfully deleted; false if the joke wasn't found</returns>
    public async Task<bool> DeleteJoke(Guid id, CancellationToken ct = default)
    {
        var joke = await _db.Jokes.FirstOrDefaultAsync(j => j.Id == id, ct);

        if (joke == null)
        {
            _logger.LogInformation("Joke with ID {JokeId} not found", id);
            return false;
        }

        _db.Jokes.Remove(joke);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// Retrieves all jokes from the database, including their associated tags.
    /// </summary>
    /// <remarks>
    /// This method provides a complete dataset retrieval with these characteristics:
    /// - Includes all related tag data
    /// - Uses AsNoTracking for optimal read performance
    /// - Returns the entire joke collection in a single query
    /// 
    /// Performance considerations:
    /// - Suitable for smaller datasets
    /// - For large datasets, consider using pagination instead
    /// - Uses eager loading for tags to prevent N+1 query issues
    /// </remarks>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>A list of all jokes with their associated tags</returns>
    public async Task<List<Joke>?> GetJokesAsync(CancellationToken ct = default) =>
        await _db.Jokes
            .Include(j => j.Tags)
            .Include(j => j.User)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <summary>
    /// Updates an existing joke entity with partial update support and tag management.
    /// </summary>
    /// <param name="joke">The joke entity containing updated values</param>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>The updated joke if found and updated; otherwise, null</returns>
    public async Task<Joke?> UpdateJoke(Joke? joke, CancellationToken ct = default)
    {
        if (joke == null) return null;

        var existingJoke = await _db.Jokes
            .Include(j => j.Tags)
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.Id == joke.Id, ct);

        if (existingJoke == null)
        {
            _logger.LogInformation("Joke with ID {JokeId} not found", joke.Id);
            return null;
        }

        existingJoke.MapObject<Joke>(joke);

        if (joke.Tags?.Count > 0)
        {
            var newTags = joke.Tags.ToList();
            existingJoke.Tags?.Clear();

            foreach (var tag in newTags)
            {
                var tagEntity = await _tagService.CreateOrFindTagAsync(tag.Name, ct);
                existingJoke?.Tags?.Add(tagEntity);
            }
        }

        await _db.SaveChangesAsync(ct);

        return existingJoke;
    }

    /// <summary>
    /// Performs a case-insensitive search across joke content and tags.
    /// </summary>
    /// <remarks>
    /// This method implements a flexible search mechanism that:
    /// 
    /// Search Features:
    /// - Searches both joke content and tag names
    /// - Performs case-insensitive matching
    /// - Handles empty or null queries gracefully
    /// 
    /// Query Optimization:
    /// - Uses Entity Framework's Include for efficient tag loading
    /// - Combines multiple search conditions in a single query
    /// - Returns empty list for empty search terms
    /// 
    /// The search is performed using SQL LIKE operations, translated from
    /// the Contains method calls. This provides efficient partial matching
    /// capabilities at the database level.
    /// </remarks>
    /// <param name="query">The search term to match against jokes and tags</param>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>A list of jokes matching the search criteria in either content or tags</returns>
    public async Task<List<Joke>?> SearchJokes(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(query)) return [];

        query = query.ToLower().Trim();

        return await _db.Jokes
            .Include(j => j.Tags)
            .Include(j => j.User)
            .Where(joke => joke.Content.Contains(query) ||
                           joke.Tags.Any(t => t.Name.Contains(query)))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Performs a comprehensive search and filter operation on jokes with multiple criteria.
    /// </summary>
    /// <remarks>
    /// This advanced query method supports:
    /// 
    /// Search Capabilities:
    /// - Full-text search across joke content
    /// - Tag-based search
    /// - Case-insensitive matching
    /// 
    /// Filter Options:
    /// - Type-based filtering
    /// - Active status filtering
    /// - Custom sort ordering
    /// </remarks>
    /// <param name="request">Filter parameters including search term, type, and pagination info</param>
    /// <param name="ct">Cancellation token for operation cancellation</param>
    /// <returns>A filtered and sorted list of jokes matching the criteria</returns>
    public async Task<List<Joke>> SearchAndFilter(FilterRequest request, CancellationToken ct = default)
    {
        // Start with the base query including tags
        var query = _db.Jokes
            .Include(j => j.Tags)
            .Include(j => j.User)
            .AsNoTracking();

        // Apply type filter if specified
        if (request.Type != null)
            query = query.Where(j => j.Type == request.Type);

        // Apply text search if query is provided
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerm = request.Query.ToLower().Trim();
            query = query.Where(joke =>
                joke.Content.ToLower().Contains(searchTerm) ||
                joke.Tags.Any(t => t.Name.ToLower().Contains(searchTerm)));
        }

        if (request.Active.HasValue)
        {
            query = query.Where(j => j.IsActive == request.Active);
        }

        // Apply sorting based on request
        query = !string.IsNullOrEmpty(request.SortBy)
            ? query.ApplySortingWithTags(request.SortBy, request.SortDescending ?? false)
            : query.OrderByDescending(j => j.CreatedAt); // Default sorting by creation date if no sort specified


        // Apply pagination if specified
        query = query.ApplyPaging(request.PageNumber, request.PageSize);

        // Execute the query and return results
        var result = await query.ToListAsync(ct);

        _logger.LogInformation(
            "Search and filter returned {Count} jokes for Type={Type}, Query={Query}, Sort={Sort}",
            result.Count,
            request.Type,
            request.Query ?? "none",
            request.SortBy ?? "default");

        return result;
    }
}