using System.Text.Json;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Features.Jokes;
using whateverAPI.Helpers;

namespace whateverAPI.Services;

public class JokeApiService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private readonly ILogger<JokeApiService> _logger;

    public JokeApiService(HttpClient httpClient, ILogger<JokeApiService> logger, AppDbContext db)
    {
        _httpClient = httpClient;
        _logger = logger;
        _db = db;
    }

    public async Task<Joke?> GetExternalJoke(bool includeNsfw = true)
    {
        try
        {
            var response = await _httpClient.GetAsync($"joke/dark");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch joke from JokeAPI: {StatusCode}", response.StatusCode);
                return null;
            }

            var jokeResponse = await response.Content.ReadFromJsonAsync<JokeApiResponse>();
            // var jokeResponse = JsonSerializer.Deserialize<JokeApiResponse>(content);

            if (jokeResponse == null || jokeResponse.Error) return null;

            var joke = EntityMapper.JokeApiResponseToJoke(jokeResponse);

            _db.Jokes.Add(joke);
            await _db.SaveChangesAsync();
            // Convert external joke format to your internal Joke model
            return joke;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching joke from external API");
            return null;
        }
    }
}