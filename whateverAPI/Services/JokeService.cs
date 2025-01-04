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

    Task<List<Joke>?> SearchJokes(string query);

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

    Task<bool> DeleteJoke(Guid id);

    Task<List<Joke>?> GetJokes();

    Task<Joke?> UpdateJoke(Joke? joke);
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
                    // tag.Name = tag.Name.ToLower();
                    var existingTag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == tag.Name);

                    if (existingTag == null)
                    {
                        // Tag doesn't exist, add it to the database
                        _db.Tags.Add(tag);
                        joke.Tags.Add(tag);
                    }
                    else
                    {
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
            var query = _db.Jokes.Include(j => j.Tags).Where(j => j.Type == type);

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

    public async Task<bool> JokeExistsById(Guid id) => await _db.Jokes.AnyAsync(j => j.Id == id);

    public async Task<int> GetJokesCountByType(JokeType type) => await _db.Jokes.CountAsync(j => j.Type == type);

    public async Task<bool> DeleteJoke(Guid id)
    {
        try
        {
            var joke = await _db.Jokes.FirstOrDefaultAsync(j => j.Id == id);

            if (joke == null)
            {
                _logger.LogInformation("Joke with ID {JokeId} not found", id);
                return false;
            }

            _db.Jokes.Remove(joke);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted joke with ID: {JokeId}", id);

            return true;
        }

        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<Joke>?> GetJokes()
    {
        try
        {
            _logger.LogInformation("Retrieving all jokes");
            return await _db.Jokes.Include(j => j.Tags).AsNoTracking().ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Joke?> UpdateJoke(Joke? joke)
    {
        try
        {
            var existingJoke = await _db.Jokes
                .Include(j => j.Tags)
                .FirstOrDefaultAsync(j => j.Id == joke.Id);

            if (existingJoke == null) return null;

            // Update basic properties
            existingJoke.Content = joke?.Content ?? existingJoke.Content;
            existingJoke.Type = joke?.Type ?? existingJoke.Type;
            existingJoke.LaughScore = joke?.LaughScore ?? existingJoke.LaughScore;


            // Handle tags if provided
            if (joke?.Tags?.Count > 0)
            {
                existingJoke.Tags?.Clear();
                foreach (var tag in joke.Tags)
                {
                    var existingTag = await _db.Tags
                        .FirstOrDefaultAsync(t => t.Name == tag.Name);

                    if (existingTag == null)
                    {
                        tag.Id = Guid.CreateVersion7();
                        _db.Tags.Add(tag);
                        existingJoke.Tags?.Add(tag);
                    }
                    else
                    {
                        existingJoke.Tags?.Add(existingTag);
                    }
                }
            }

            await _db.SaveChangesAsync();
            return existingJoke;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating joke with ID: {JokeId}", joke.Id);
            throw;
        }
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

    public async Task<List<Joke>?> SearchJokes(string query)
    {
        try
        {
            if (string.IsNullOrEmpty(query)) return [];

            query = query.Trim().ToLower();

            // This query will work because it lets the database handle the case-insensitive comparison
            var searchResults = await _db.Jokes
                .Include(j => j.Tags)
                .Where(joke => joke.Content.Contains(query) || joke.Tags.Any(t => t.Name.Contains(query)))
                .ToListAsync();

            _logger.LogInformation("Search for '{Query}' returned {Count} results", query, searchResults.Count);

            return searchResults;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}