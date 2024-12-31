using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using whateverAPI.Entities;

namespace whateverAPI.Data;

public class JokeRepository : BaseRepository<Joke, Guid>
{
    public JokeRepository(AppDbContext context, ILogger<JokeRepository> logger)
        : base(context, logger)
    {
    }

    // Override base methods to include Tags
    public override async Task<Joke?> GetByIdAsync(Guid id)
    {
        await CountAsync();
        
        return await DbSet
            .Include(j => j.Tags)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public override async Task<IEnumerable<Joke>> GetAllAsync()
    {
        return await DbSet
            .Include(j => j.Tags)
            .ToListAsync();
    }

    public override async Task<Joke> CreateAsync(Joke joke)
    {
        try
        {
            if (joke.Tags?.Count > 0)
            {
                var newTags = joke.Tags.ToList();
                joke.Tags.Clear();

                foreach (var tag in newTags)
                {
                    var existingTag = await Context.Tags
                        .FirstOrDefaultAsync(t => t.Name == tag.Name);

                    joke.Tags.Add(existingTag ?? tag);
                }
            }

            await DbSet.AddAsync(joke);
            await Context.SaveChangesAsync();

            return await DbSet
                .Include(j => j.Tags)
                .FirstAsync(j => j.Id == joke.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating joke: {Content}", joke.Content);
            throw;
        }
    }

    // Implement IJokeRepository-specific methods
    public async Task<Joke?> GetRandomAsync()
    {
        var count = await DbSet.CountAsync();
        if (count == 0) return null;

        var random = new Random();
        var skipCount = random.Next(count);

        return await DbSet
            .Include(j => j.Tags)
            .Skip(skipCount)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Joke>> GetByTypeAsync(
        JokeType type,
        int? pageSize = null,
        int? pageNumber = null,
        string? sortBy = null,
        bool sortDescending = false)
    {
        var query = DbSet
            .Include(j => j.Tags)
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
            query = ApplySorting(query, keySelector, sortDescending);
        }

        if (pageSize.HasValue && pageNumber.HasValue)
        {
            query = ApplyPaging(query, pageNumber.Value, pageSize.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<int> CountByTypeAsync(JokeType type)
    {
        return await DbSet.CountAsync(j => j.Type == type);
    }
}