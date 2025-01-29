using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Services;

namespace whateverAPI.Endpoints;

public class JokeEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api");
        var jokeGroup = apiGroup.MapGroup("/jokes").WithTags("Jokes");

        // Get All Jokes
        jokeGroup.MapGet("/", async Task<IResult> (
                JokeService jokeService,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var jokes = await jokeService.GetJokesAsync(ct);
                return jokes is not null && jokes.Count != 0
                    // ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
                    ? TypedResults.Ok(Joke.ToJokeResponses(jokes))
                    : context.CreateNotFoundProblem(nameof(Joke), "all");
            })
            .WithName("GetJokesAsync")
            .WithDescription("Retrieves all jokes from the database with pagination")
            .WithSummary("Get all jokes")
            .WithOpenApi()
            .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
// .RequireAuthorization();

// Get Joke by ID
        jokeGroup.MapGet("/{id:guid}", async Task<IResult> (
                [FromRoute] Guid id,
                JokeService jokeService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var joke = await jokeService.GetJokeById(id, ct);
                return joke is not null
                    ? TypedResults.Ok(Joke.ToResponse(joke))
                    // ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
                    : context.CreateNotFoundProblem(nameof(Joke), id.ToString());
            })
            .WithName("GetJokeById")
            .WithDescription("Retrieves a specific joke by its unique identifier")
            .WithSummary("Get a joke by ID")
            .WithOpenApi()
            .Produces<JokeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

// Create New Joke
        jokeGroup.MapPost("/", async Task<IResult> (
                CreateJokeRequest request,
                JokeService jokeService,
                HttpContext context,
                UserService userService,
                CancellationToken ct) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                User? user = null;

                if (userId != null && Guid.TryParse(userId, out var userGuid))
                {
                    user = await userService.GetUserById(userGuid, ct);
                }

                var joke = Joke.FromCreateRequest(request, user);
                // var joke = Mapper.CreateRequestToJoke(request);
                var created = await jokeService.CreateJoke(joke, ct);
                var response = Joke.ToResponse(created);
                // var response = JokeResponse.FromJoke(created);
                // var response = Mapper.JokeToJokeResponse(created);
                return response is not null
                    ? TypedResults.Created($"/api/jokes/{created.Id}", response)
                    : context.CreateUnprocessableEntityProblem("Create Joke");
            })
            .WithName("CreateJoke")
            .WithDescription("Creates a new joke with the provided content and metadata")
            .WithSummary("Create a new joke")
            .WithOpenApi()
            .Accepts<CreateJokeRequest>("application/json")
            .Produces<JokeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ValidationFilter<CreateJokeRequest>>()
            .RequireAuthorization();

// Update Joke
        jokeGroup.MapPut("/{id:guid}", async Task<IResult> (
                [FromRoute] Guid id,
                UpdateJokeRequest request,
                JokeService jokeService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var joke = Joke.FromUpdateRequest(id, request);
                // var joke = Mapper.UpdateRequestToJoke(id, request);
                var updated = await jokeService.UpdateJoke(joke, ct);
                return updated is not null
                    ? TypedResults.Ok(Joke.ToResponse(updated))
                    // ? TypedResults.Ok(Mapper.JokeToJokeResponse(updated))
                    : context.CreateNotFoundProblem(nameof(Joke), id.ToString());
            })
            .WithName("UpdateJoke")
            .WithDescription("Updates an existing joke's content and metadata")
            .WithSummary("Update a joke")
            .WithOpenApi()
            .Accepts<UpdateJokeRequest>("application/json")
            .Produces<JokeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<UpdateJokeRequest>>();

// Delete Joke
        jokeGroup.MapDelete("/{id:guid}", async Task<IResult> (
                [FromRoute] Guid id,
                JokeService jokeService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var result = await jokeService.DeleteJoke(id, ct);
                return result
                    ? TypedResults.NoContent()
                    : context.CreateNotFoundProblem(nameof(Joke), id.ToString());
            })
            .WithName("DeleteJoke")
            .WithDescription("Permanently removes a joke from the database")
            .WithSummary("Delete a joke")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);


        jokeGroup.MapGet("/random", async Task<IResult> (
                JokeService jokeService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var joke = await jokeService.GetRandomJoke(ct);
                return joke is not null
                    ? TypedResults.Ok(Joke.ToResponse(joke))
                    // ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
                    : context.CreateNotFoundProblem(nameof(Joke), "random");
            })
            .WithName("GetRandomJoke")
            .WithDescription("Retrieves a random joke from the available collection")
            .WithSummary("Get a random joke")
            .WithOpenApi()
            .Produces<JokeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        jokeGroup.MapGet("/klump", async Task<IResult> (
                JokeService jokeService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var joke = await jokeService.GetRandomJoke(ct);
                return joke is not null
                    ? TypedResults.Ok(joke.Content.Replace("\n", " "))
                    // ? TypedResults.Ok(Mapper.JokeToJokeResponse(joke))
                    : context.CreateNotFoundProblem(nameof(Joke), "klump");
            })
            .WithName("Klump")
            .WithDescription("Klump")
            .WithSummary("Klump")
            .WithOpenApi()
            .Produces<string>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

// Search Jokes
        jokeGroup.MapGet("/search", async Task<IResult> (
                [FromQuery(Name = "q")] string query,
                JokeService jokeService,
                HttpContext context,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return context.CreateBadRequestProblem("Search query cannot be empty");
                }

                var jokes = await jokeService.SearchJokes(query, ct);
                return jokes?.Count > 0
                    ? TypedResults.Ok(Joke.ToJokeResponses(jokes))
                    // ? TypedResults.Ok(Mapper.JokesToJokeReponses(jokes))
                    : context.CreateNotFoundProblem(nameof(Joke), $"matching query '{query}'");
            })
            .WithName("SearchJokes")
            .WithDescription("Searches for jokes containing the specified query in their content or tags")
            .WithSummary("Search for jokes")
            .WithOpenApi()
            .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);


        jokeGroup.MapPost("/find", async Task<IResult> (
                [AsParameters] FilterRequest request,
                JokeService jokeService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var jokes = await jokeService.SearchAndFilter(request, ct);
                return jokes.Count != 0
                    ? TypedResults.Ok(Joke.ToJokeResponses(jokes))
                    : context.CreateNotFoundProblem(nameof(Joke),
                        $"matching criteria (Type={request.Type}, Query={request.Query ?? "none"})");
            })
            .WithName("SearchAndFilterJokes")
            .WithDescription("Searches and filters jokes with optional text search, type filtering, sorting, and pagination")
            .WithSummary("Search and filter jokes")
            .WithOpenApi()
            .Produces<List<JokeResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ValidationFilter<FilterRequest>>();

// Get External Joke
        jokeGroup.MapGet("/whatever", async Task<IResult> (
                JokeApiService jokeApiService,
                HttpContext context,
                CancellationToken ct) =>
            {
                try
                {
                    var joke = await jokeApiService.GetExternalJoke(ct);
                    return joke is not null
                        ? TypedResults.Ok(joke)
                        : context.CreateNotFoundProblem("External Joke", "random");
                }
                catch (HttpRequestException ex)
                {
                    return context.CreateExternalServiceProblem(
                        "Joke API",
                        "Failed to fetch joke from external service",
                        ex);
                }
            })
            .WithName("GetWhateverJoke")
            .WithDescription("Retrieves a random joke from a third-party API")
            .WithSummary("Get a joke from a third-party API")
            .WithOpenApi()
            .Produces<Joke>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }
}