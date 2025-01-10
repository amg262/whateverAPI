using Microsoft.EntityFrameworkCore;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Models;

namespace whateverAPI.Services;

// public interface ITagService
// {
//     Task<List<Tag>> GetAllTagsAsync(CancellationToken ct = default);
//     Task<Tag?> GetTagByIdAsync(Guid id, CancellationToken ct = default);
//     Task<Tag> CreateTagAsync(CreateTagRequest request, CancellationToken ct = default);
//     Task<Tag?> UpdateTagAsync(Guid id, UpdateTagRequest request, CancellationToken ct = default);
//     Task<bool> DeleteTagAsync(Guid id, CancellationToken ct = default);
//     Task<Tag> CreateOrFindByNameAsync(string tagName, CancellationToken ct = default);
// }

public class TagService //: ITagService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TagService> _logger;

    public TagService(AppDbContext db, ILogger<TagService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Tag>> GetAllTagsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.Tags
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tags");
            throw;
        }
    }

    public async Task<Tag?> GetTagByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _db.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag by ID: {TagId}", id);
            throw;
        }
    }

    public async Task<Tag> CreateOrFindTagAsync(string tagName, CancellationToken ct = default)
    {
        try
        {
            // Check if tag with same name already exists
            var existingTag = await _db.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower().Trim() == tagName.ToLower().Trim(), ct);

            if (existingTag != null) return existingTag;

            var newTag = Tag.FromName(tagName);

            _db.Tags.Add(newTag);
            await _db.SaveChangesAsync(ct);

            return newTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag: {TagName}", tagName);
            throw;
        }
    }

    public async Task<Tag> CreateTagAsync(CreateTagRequest request, CancellationToken ct = default)
    {
        try
        {
            // Check if tag with same name already exists
            var existingTag = await _db.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower().Trim() == request.Name.ToLower().Trim(), ct);

            if (existingTag != null)
            {
                throw new InvalidOperationException($"Tag with name '{request.Name}' already exists");
            }

            var tag = Tag.FromCreateRequest(request);

            _db.Tags.Add(tag);
            await _db.SaveChangesAsync(ct);

            return tag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag: {TagName}", request.Name);
            throw;
        }
    }

    public async Task<Tag?> UpdateTagAsync(Guid id, UpdateTagRequest request, CancellationToken ct = default)
    {
        try
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (tag == null) return null;

            // Check if new name conflicts with existing tag
            var existingTag = await _db.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower().Trim() == request.Name.ToLower().Trim() && t.Id != id, ct);

            if (existingTag != null)
            {
                throw new InvalidOperationException($"Tag with name '{request.Name}' already exists");
            }

            tag.ApplyUpdate(request);
            tag.ModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return tag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag: {TagId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteTagAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (tag == null) return false;

            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync(ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag: {TagId}", id);
            throw;
        }
    }

    public async Task<Tag> CreateOrFindByNameAsync(string tagName, CancellationToken ct = default)
    {
        try
        {
            var existingTag = await _db.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower().Trim() == tagName.ToLower().Trim(), ct);

            if (existingTag != null) return existingTag;

            var newTag = Tag.FromName(tagName);
            _db.Tags.Add(newTag);
            await _db.SaveChangesAsync(ct);

            return newTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or finding tag: {TagName}", tagName);
            throw;
        }
    }
}