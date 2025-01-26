using Microsoft.AspNetCore.Mvc;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Services;

namespace whateverAPI.Endpoints;

public class TagEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api");
        var tagGroup = apiGroup.MapGroup("/tag").WithTags("Tags");


// Get all tags
        tagGroup.MapGet("/", async Task<IResult> (
                TagService tagService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var tags = await tagService.GetAllTagsAsync(ct);
                return tags.Count != 0
                    ? TypedResults.Ok(Tag.ToTagResponses(tags))
                    : context.CreateNotFoundProblem(nameof(Tag), "all");
            })
            .WithName("GetTags")
            .WithDescription("Retrieves all tags")
            .WithSummary("Get all tags")
            .WithOpenApi()
            .Produces<List<TagResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

// Get tag by ID
        tagGroup.MapGet("/{id:guid}", async Task<IResult> (
                [FromRoute] Guid id,
                TagService tagService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var tag = await tagService.GetTagByIdAsync(id, ct);
                return tag is not null
                    ? TypedResults.Ok(Tag.ToResponse(tag))
                    : context.CreateNotFoundProblem(nameof(Tag), id.ToString());
            })
            .WithName("GetTagById")
            .WithDescription("Retrieves a specific tag by its unique identifier")
            .WithSummary("Get a tag by ID")
            .WithOpenApi()
            .Produces<TagResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

// Create new tag
        tagGroup.MapPost("/", async Task<IResult> (
                CreateTagRequest request,
                TagService tagService,
                HttpContext context,
                CancellationToken ct) =>
            {
                try
                {
                    var tag = await tagService.CreateTagAsync(request, ct);

                    return TypedResults.Created($"/api/tags/{tag.Id}", Tag.ToResponse(tag));
                }
                catch (InvalidOperationException ex)
                {
                    return context.CreateBadRequestProblem(ex.Message);
                }
            })
            .WithName("CreateTag")
            .WithDescription("Creates a new tag")
            .WithSummary("Create a tag")
            .WithOpenApi()
            .Accepts<CreateTagRequest>("application/json")
            .Produces<TagResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ValidationFilter<CreateTagRequest>>();

// Update tag
        tagGroup.MapPut("/{id:guid}", async Task<IResult> (
                [FromRoute] Guid id,
                UpdateTagRequest request,
                TagService tagService,
                HttpContext context,
                CancellationToken ct) =>
            {
                try
                {
                    var tag = await tagService.UpdateTagAsync(id, request, ct);
                    return tag is not null
                        ? TypedResults.Ok(Tag.ToResponse(tag))
                        : context.CreateNotFoundProblem(nameof(Tag), id.ToString());
                }
                catch (InvalidOperationException ex)
                {
                    return context.CreateBadRequestProblem(ex.Message);
                }
            })
            .WithName("UpdateTag")
            .WithDescription("Updates an existing tag")
            .WithSummary("Update a tag")
            .WithOpenApi()
            .Accepts<UpdateTagRequest>("application/json")
            .Produces<TagResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ValidationFilter<UpdateTagRequest>>();

// Delete tag
        tagGroup.MapDelete("/{id:guid}", async Task<IResult> (
                [FromRoute] Guid id,
                TagService tagService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var result = await tagService.DeleteTagAsync(id, ct);
                return result
                    ? TypedResults.NoContent()
                    : context.CreateNotFoundProblem(nameof(Tag), id.ToString());
            })
            .WithName("DeleteTag")
            .WithDescription("Deletes a tag")
            .WithSummary("Delete a tag")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}