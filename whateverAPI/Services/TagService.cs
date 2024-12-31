using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Features.Tags.CreateTag;

namespace whateverAPI.Services;

public interface ITagService
{
    Task<Tag> CreateTag(Request createTagRequest);
}

public class TagService : ITagService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TagService> _logger;

    public TagService(AppDbContext db, ILogger<TagService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<Tag> CreateTag(Request createTagRequest)
    {
        throw new NotImplementedException();
    }
}