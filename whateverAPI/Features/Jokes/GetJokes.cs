﻿using FastEndpoints;
using whateverAPI.Helpers;
using whateverAPI.Services;

namespace whateverAPI.Features.Jokes;

public class GetJokes : EndpointWithoutRequest<List<JokeResponse>>
{
    private readonly IJokeService _jokeService;

    public GetJokes(IJokeService jokeService)
    {
        _jokeService = jokeService;
    }
    
    public override void Configure()
    {
        Get("/jokes");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get jokes";
            s.Description = "Get all jokes";
            s.Response<List<JokeResponse>>(200, "Jokes found successfully");
        });
    }
    
    public override async Task HandleAsync(CancellationToken ct)
    {
        var jokes = await _jokeService.GetJokes();
        var response = EntityMapper.JokesToJokeReponses(jokes);

        await SendAsync(response, cancellation: ct);
    }
}