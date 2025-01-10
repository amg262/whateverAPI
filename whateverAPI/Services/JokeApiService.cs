using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;

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

    public async Task<Joke?> GetExternalJoke(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"joke/dark", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch joke from JokeAPI: {StatusCode}", response.StatusCode);
                return null;
            }

            var jokeResponse = await response.Content.ReadFromJsonAsync<JokeApiResponse>(ct);
            // var jokeResponse = JsonSerializer.Deserialize<JokeApiResponse>(content);

            if (jokeResponse == null || jokeResponse.Error) return null;

            // var joke = Mapper.JokeApiResponseToJoke(jokeResponse);
            var joke = Joke.FromJokeApiResponse(jokeResponse);

            _db.Jokes.Add(joke);
            await _db.SaveChangesAsync(ct);
            return joke;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Fetching joke from external API was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching joke from external API");
            throw;
        }
    }
}