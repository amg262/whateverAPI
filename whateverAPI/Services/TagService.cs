using System.Data;
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
/// <summary>
/// Service responsible for managing tag operations in the WhateverAPI system.
/// Provides functionality for creating, reading, updating, and deleting tags,
/// as well as handling tag relationships and validation.
/// </summary>
public class TagService //: ITagService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TagService> _logger;

    /// <summary>
    /// Initializes a new instance of the TagService.
    /// </summary>
    /// <param name="db">The database context for tag operations</param>
    /// <param name="logger">Logger instance for tracking service operations</param>
    public TagService(AppDbContext db, ILogger<TagService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all tags from the database, ordered alphabetically by name.
    /// </summary>
    /// <param name="ct">Cancellation token for async operations</param>
    /// <returns>A list of all tags in the system</returns>
    /// <exception cref="Exception">Thrown when database operations fail</exception>
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

    /// <summary>
    /// Retrieves a specific tag by its unique identifier.
    /// </summary>
    /// <param name="id">The GUID of the tag to retrieve</param>
    /// <param name="ct">Cancellation token for async operations</param>
    /// <returns>The requested tag if found; null otherwise</returns>
    /// <exception cref="Exception">Thrown when database operations fail</exception>
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

    /// <summary>
    /// Creates a new tag or returns an existing one with the same name.
    /// This method ensures tag name uniqueness while preventing duplicates.
    /// </summary>
    /// <param name="tagName">The name of the tag to create or find</param>
    /// <param name="ct">Cancellation token for async operations</param>
    /// <returns>The created or existing tag</returns>
    /// <exception cref="Exception">Thrown when database operations fail</exception>
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

    /// <summary>
    /// Creates a new tag based on a creation request.
    /// Enforces unique tag names by throwing an exception if a duplicate is detected.
    /// </summary>
    /// <param name="request">The tag creation request containing the tag details</param>
    /// <param name="ct">Cancellation token for async operations</param>
    /// <returns>The newly created tag</returns>
    /// <exception cref="DuplicateNameException">Thrown when a tag with the same name already exists</exception>
    /// <exception cref="Exception">Thrown when database operations fail</exception>
    public async Task<Tag> CreateTagAsync(CreateTagRequest request, CancellationToken ct = default)
    {
        try
        {
            // Check if tag with same name already exists
            var existingTag = await _db.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower().Trim() == request.Name.ToLower().Trim(), ct);

            if (existingTag != null)
            {
                throw new DuplicateNameException($"Tag with name '{request.Name}' already exists");
            }

            var newTag = Tag.FromCreateRequest(request);

            _db.Tags.Add(newTag);
            await _db.SaveChangesAsync(ct);

            return newTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag: {TagName}", request.Name);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing tag with new information.
    /// Ensures unique tag names are maintained during the update.
    /// </summary>
    /// <param name="id">The GUID of the tag to update</param>
    /// <param name="request">The update request containing the new tag details</param>
    /// <param name="ct">Cancellation token for async operations</param>
    /// <returns>The updated tag if found; null otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown when the new name conflicts with an existing tag</exception>
    /// <exception cref="Exception">Thrown when database operations fail</exception>
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

            // var updatedTag = Tag.FromUpdateRequest(request);
            tag.ApplyUpdate(request);
            await _db.SaveChangesAsync(ct);

            return tag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag: {TagId}", id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a tag from the system by its ID.
    /// </summary>
    /// <param name="id">The GUID of the tag to delete</param>
    /// <param name="ct">Cancellation token for async operations</param>
    /// <returns>True if the tag was successfully deleted; false if the tag was not found</returns>
    /// <exception cref="Exception">Thrown when database operations fail</exception>
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

    /// <summary>
    /// Creates a new tag or finds an existing one by name.
    /// This method is similar to CreateOrFindTagAsync but uses a different naming convention
    /// for backward compatibility or specific use cases.
    /// </summary>
    /// <param name="tagName">The name of the tag to create or find</param>
    /// <param name="ct">Cancellation token for async operations</param>
    /// <returns>The created or existing tag</returns>
    /// <exception cref="Exception">Thrown when database operations fail</exception>
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