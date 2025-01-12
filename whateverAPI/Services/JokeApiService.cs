using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;

namespace whateverAPI.Services;

/// <summary>
/// Provides functionality to fetch jokes from an external API, process them, and store them in the local database.
/// This service handles the complete workflow of joke retrieval, tag processing, and persistence.
/// </summary>
public class JokeApiService
{
    private readonly HttpClient _httpClient;
    private readonly TagService _tagService;
    private readonly AppDbContext _db;
    private readonly ILogger<JokeApiService> _logger;

    /// <summary>
    /// Initializes a new instance of the JokeApiService with required dependencies.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests to the external joke API</param>
    /// <param name="logger">Logger for tracking operations and errors</param>
    /// <param name="db">Database context for persisting jokes and related data</param>
    /// <param name="tagService">Service for managing joke tags</param>
    public JokeApiService(HttpClient httpClient, ILogger<JokeApiService> logger, AppDbContext db, TagService tagService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _db = db;
        _tagService = tagService;
    }

    /// <summary>
    /// Retrieves a joke from the external API, processes it, and stores it in the database.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation if needed</param>
    /// <returns>
    /// A Task that represents the asynchronous operation. The task result contains:
    /// - The newly created Joke entity if successful
    /// - null if the API request fails or returns invalid data
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the cancellation token
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when an unexpected error occurs during API communication or data processing
    /// </exception>
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
            var newJoke = Joke.FromJokeApiResponse(jokeResponse);

            // Clear the tags that were created in FromJokeApiResponse
            var tagNames = newJoke.Tags?.Select(t => t.Name).ToList() ?? [];
            newJoke.Tags?.Clear();

            // Add each tag using the TagService
            foreach (var tagName in tagNames)
            {
                var tagEntity = await _tagService.CreateOrFindTagAsync(tagName, ct);
                newJoke.Tags ??= [];
                newJoke.Tags.Add(tagEntity);
            }

            _db.Jokes.Add(newJoke);
            await _db.SaveChangesAsync(ct);
            return newJoke;
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