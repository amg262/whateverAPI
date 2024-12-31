using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;

namespace whateverAPI.Services;

public interface IJokeService
{
    // Create operations
    Task<Joke> CreateJoke(Joke joke);

    // Read operations
    Task<Joke?> GetRandomJoke();

    Task<List<Joke>> GetJokesByType(
        JokeType type,
        int? pageSize = null,
        int? pageNumber = null,
        string? sortBy = null,
        bool sortDescending = false);

    // Additional utility methods
    Task<bool> JokeExistsById(Guid id);
    Task<int> GetJokesCountByType(JokeType type);

    Task<Joke?> GetJokeById(Guid id);
}

public class JokeService : IJokeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<JokeService> _logger;

    public JokeService(AppDbContext db, ILogger<JokeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Joke> CreateJoke(Joke joke)
    {
        try
        {
            // Handle tags if provided
            if (joke.Tags?.Count > 0)
            {
                var newTags = joke.Tags.ToList();
                joke.Tags.Clear();

                foreach (var tag in newTags)
                {
                    var existingTag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == tag.Name);

                    if (existingTag == null)
                    {
                        // Tag doesn't exist, add it to the database
                        _db.Tags.Add(tag);
                        joke.Tags.Add(tag);
                    }
                    else
                    {
                        // Use the existing tag
                        joke.Tags.Add(existingTag);
                    }
                }
            }

            _db.Jokes.Add(joke);
            await _db.SaveChangesAsync();

            // Reload the joke with tags included
            return await _db.Jokes
                .Include(j => j.Tags)
                .FirstAsync(j => j.Id == joke.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating joke: {Content}", joke.Content);
            throw;
        }
    }

    public async Task<Joke?> GetRandomJoke()
    {
        try
        {
            var count = await _db.Jokes.CountAsync();
            if (count == 0)
            {
                _logger.LogInformation("No jokes available for random selection");
                return null;
            }

            var random = new Random();
            var skipCount = random.Next(count);

            var joke = await _db.Jokes
                .Include(j => j.Tags)
                .Skip(skipCount)
                .FirstOrDefaultAsync();

            if (joke != null)
            {
                _logger.LogInformation("Retrieved random joke with ID: {JokeId}", joke.Id);
            }

            return joke;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving random joke");
            throw;
        }
    }

    public async Task<List<Joke>> GetJokesByType(
        JokeType type,
        int? pageSize = null,
        int? pageNumber = null,
        string? sortBy = null,
        bool sortDescending = false)
    {
        try
        {
            var query = _db.Jokes
                .Include(j => j.Tags)
                .Where(j => j.Type == type);

            // Apply sorting if specified
            if (!string.IsNullOrEmpty(sortBy))
            {
                Expression<Func<Joke, object>> keySelector = sortBy.ToLower() switch
                {
                    "createdat" => j => j.CreatedAt,
                    "laughscore" => j => j.LaughScore ?? 0,
                    "content" => j => j.Content,
                    _ => j => j.CreatedAt
                };
                query = QueryHelper.ApplySorting(query, keySelector, sortDescending);
            }

            // Apply paging
            query = QueryHelper.ApplyPaging(query, pageNumber, pageSize);

            var result = await query.ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} jokes of type {Type} (Page: {Page}, PageSize: {PageSize}, SortBy: {SortBy}, SortDescending: {SortDescending})",
                result.Count, type, pageNumber, pageSize, sortBy, sortDescending);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jokes by type: {Type}", type);
            throw;
        }
    }

    public async Task<bool> JokeExistsById(Guid id)
    {
        return await _db.Jokes.AnyAsync(j => j.Id == id);
    }

    public async Task<int> GetJokesCountByType(JokeType type)
    {
        return await _db.Jokes.CountAsync(j => j.Type == type);
    }

    public async Task<Joke?> GetJokeById(Guid id)
    {
        try
        {
            var joke = await _db.Jokes
                .Include(j => j.Tags)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (joke == null)
            {
                _logger.LogInformation("Joke with ID {JokeId} not found", id);
                return null;
            }

            _logger.LogInformation("Retrieved joke with ID: {JokeId}", id);
            return joke;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving joke with ID: {JokeId}", id);
            throw;
        }
    }
}