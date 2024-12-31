using FastEndpoints;
using whateverAPI.Entities;

namespace whateverAPI.Features.Tags.CreateTag;

public class Mapper : Mapper<Request, Response, Tag>
{
    public override Tag ToEntity(Request r) => new() { Name = r.Name };
    public override Response FromEntity(Tag e) => new() { Id = e.Id, Name = e.Name };
}