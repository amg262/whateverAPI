﻿using FastEndpoints;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes.GetJokesByType;

public class Endpoint : Endpoint<Request, List<Response>, Mapper>
{
    private readonly IJokeService _jokeService;
    
    public Endpoint(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }
    
    public override void Configure()
    {
        Get("/jokes/type/{Type}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get jokes by type";
            s.Description = "Retrieves all jokes of a specific type with optional pagination and sorting";
            s.Response<List<Response>>(200, "Jokes retrieved successfully");
            s.Response(400, "Invalid request parameters");
            s.Response(404, "No jokes found for the specified type");
        });
        Options(o => o.WithTags("Jokes"));
    }
    
    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var jokes = await _jokeService.GetJokesByType(
            req.Type,
            req.PageSize,
            req.PageNumber,
            req.SortBy,
            req.SortDescending ?? false);

        if (!jokes.Any())
        {
            await SendNotFoundAsync(ct);
            return;
        }
        
        var response = jokes.Select(j => Mapper.FromEntity(j)).ToList();
        await SendOkAsync(response, ct);
    }
}