using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;

namespace whateverAPI.Services;

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

public class JokeService : IJokeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<JokeService> _logger;

    public JokeService(AppDbContext db, ILogger<JokeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Joke> CreateJoke(Joke joke, CancellationToken ct = default)
    {
        try
        {
            if (joke.Tags?.Count > 0)
            {
                var newTags = joke.Tags.ToList();
                joke.Tags.Clear();

                foreach (var tag in newTags)
                {
                    var existingTag = await _db.Tags
                        .FirstOrDefaultAsync(t => t.Name == tag.Name, ct);

                    if (existingTag == null)
                    {
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
            await _db.SaveChangesAsync(ct);

            return await _db.Jokes
                .Include(j => j.Tags)
                .AsNoTracking()
                .FirstAsync(j => j.Id == joke.Id, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while creating joke");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating joke: {Content}", joke.Content);
            throw;
        }
    }

    public async Task<Joke?> GetRandomJoke(CancellationToken ct = default)
    {
        try
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
                .AsNoTracking()
                .Skip(skipCount)
                .FirstOrDefaultAsync(ct);

            if (joke != null)
            {
                _logger.LogInformation("Retrieved random joke with ID: {JokeId}", joke.Id);
            }

            return joke;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while retrieving random joke");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving random joke");
            throw;
        }
    }

    public Task<List<Joke>> GetJokesByType(JokeType type, int? pageSize = null, int? pageNumber = null, string? sortBy = null,
        bool sortDescending = false, CancellationToken ct = default)
    {
        try
        {
            var query = _db.Jokes
                .Include(j => j.Tags)
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
                query = QueryHelper.ApplySorting(query, keySelector, sortDescending);
            }

            query = QueryHelper.ApplyPaging(query, pageNumber, pageSize);

            return query.ToListAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while retrieving jokes by type");
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<Joke>> GetJokesByType(FilterRequest request, CancellationToken ct = default)
    {
        try
        {
            var query = _db.Jokes
                .Include(j => j.Tags)
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
                query = QueryHelper.ApplySorting(query, keySelector, request.SortDescending);
            }

            query = QueryHelper.ApplyPaging(query, request.PageNumber, request.PageSize);

            var result = await query.ToListAsync(ct);

            _logger.LogInformation("Retrieved {Count} jokes of type {Type}", result.Count, request.Type);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while retrieving jokes by type");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jokes by type: {Type}", request.Type);
            throw;
        }
    }

    public async Task<bool> JokeExistsById(Guid id, CancellationToken ct = default) =>
        await _db.Jokes.AnyAsync(j => j.Id == id, ct);


    public async Task<int> GetJokesCountByType(JokeType type, CancellationToken ct = default) =>
        await _db.Jokes.CountAsync(j => j.Type == type, ct);


    public async Task<Joke?> GetJokeById(Guid id, CancellationToken ct = default)
    {
        try
        {
            var joke = await _db.Jokes.Include(j => j.Tags)
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == id, ct);

            if (joke != null) return joke;

            _logger.LogInformation("Joke with ID {JokeId} not found", id);
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while retrieving joke by ID");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving joke with ID: {JokeId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteJoke(Guid id, CancellationToken ct = default)
    {
        try
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while deleting joke");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting joke with ID: {JokeId}", id);
            throw;
        }
    }

    public async Task<List<Joke>?> GetJokes(CancellationToken ct = default) =>
        await _db.Jokes.Include(j => j.Tags).AsNoTracking().ToListAsync(ct);


    public async Task<Joke?> UpdateJoke(Joke? joke, CancellationToken ct = default)
    {
        try
        {
            if (joke == null) return null;

            var existingJoke = await _db.Jokes
                .Include(j => j.Tags)
                .FirstOrDefaultAsync(j => j.Id == joke.Id, ct);

            if (existingJoke == null)
            {
                _logger.LogInformation("Joke with ID {JokeId} not found", joke.Id);
                return null;
            }

            existingJoke.Content = joke.Content ?? existingJoke.Content;
            existingJoke.Type = joke.Type ?? existingJoke.Type;
            existingJoke.LaughScore = joke.LaughScore ?? existingJoke.LaughScore;

            if (joke.Tags?.Count > 0)
            {
                var newTags = joke.Tags.ToList();
                existingJoke.Tags?.Clear();

                foreach (var tag in newTags)
                {
                    var existingTag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == tag.Name, ct);

                    if (existingTag == null)
                    {
                        _db.Tags.Add(tag);
                        existingJoke.Tags?.Add(tag);
                    }
                    else
                    {
                        existingJoke.Tags?.Add(existingTag);
                    }
                }
            }

            await _db.SaveChangesAsync(ct);

            return existingJoke;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while updating joke");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating joke with ID: {JokeId}", joke?.Id);
            throw;
        }
    }

    public async Task<List<Joke>?> SearchJokes(string query, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(query)) return [];

            query = query.Trim().ToLower();

            return await _db.Jokes
                .Include(j => j.Tags)
                .Where(joke => joke.Content.Contains(query) ||
                               joke.Tags.Any(t => t.Name.Contains(query)))
                .ToListAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while searching jokes");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching jokes with query: {Query}", query);
            throw;
        }
    }
}