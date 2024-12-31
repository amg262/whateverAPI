using FastEndpoints;
using whateverAPI.Services;

namespace whateverAPI.Features.Tags.CreateTag;

public class Endpoint : Endpoint<Request, Response, Mapper>
{
    private readonly ITagService _tagService;

    public Endpoint(ITagService tagService)
    {
        _tagService = tagService;
    }

    public override void Configure()
    {
        Post("/tags/create");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new tag";
            s.Description = "Creates a new tag entry with name";
            s.Response<Response>(201, "Tag created successfully");
            s.Response(400, "Invalid request");
        });
    }
    
    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // var joke = await _tagService.CreateJoke(req);
        // Response response = Map.FromEntity(joke);
        //
        //
        // await SendCreatedAtAsync<GetJoke.Endpoint>(
        //     new { id = joke.Id },
        //     response,
        //     cancellation: ct
        // );
    }
}