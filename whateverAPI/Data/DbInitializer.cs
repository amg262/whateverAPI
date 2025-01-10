using Microsoft.EntityFrameworkCore;
using Polly;
using whateverAPI.Entities;

namespace whateverAPI.Data;

/// <summary>
/// Provides functionality to initialize and seed the database with joke data.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initializes the database with a retry policy that uses exponential backoff.
    /// This is particularly useful in containerized environments where the database
    /// might take some time to become available.
    /// </summary>
    /// <param name="app">The WebApplication instance to extend</param>
    /// <param name="maxRetryAttempts">Maximum number of retry attempts (default: 50)</param>
    /// <param name="maxDelaySeconds">Maximum delay between retries in seconds (default: 30)</param>
    /// <returns>The WebApplication instance for method chaining</returns>
    public static async Task InitializeDatabaseRetryAsync(this WebApplication app, int maxRetryAttempts = 50,
        int maxDelaySeconds = 30)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .Or<Npgsql.NpgsqlException>()
            .Or<System.Net.Sockets.SocketException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(maxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt), maxDelaySeconds)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    app.Logger.LogWarning(
                        // exception,
                        "Attempt {RetryCount} of {MaxRetries} failed to connect to database, {Exception} Waiting {TimeSpan} before next attempt",
                        retryCount,
                        maxRetryAttempts,
                        exception.InnerException?.Message ?? exception.Message,
                        timeSpan);
                });

        var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await retryPolicy.ExecuteAsync(async () =>
        {
            await context.Database.MigrateAsync();
            await InitDb(app);
        });
    }

    /// <summary>
    /// Initializes and seeds the joke database at application startup.
    /// Applies any pending migrations and seeds initial data if necessary.
    /// </summary>
    private static async Task InitDb(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await SeedData(scope.ServiceProvider.GetService<AppDbContext>());
    }

    private static async Task SeedData(AppDbContext? context)
    {
        if (context == null)
        {
            Console.WriteLine("Database context is null - skipping seed");
            return;
        }

        await context.Database.MigrateAsync();

        // Check if we already have joke data
        if (context.Jokes.Any())
        {
            Console.WriteLine("Joke database already contains data - skipping seed");
            return;
        }

        // Create some common tags that can be reused across jokes
        var programmingTag = new Tag
            { Id = Guid.CreateVersion7(), Name = "Programming", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow };
        var dadJokeTag = new Tag
            { Id = Guid.CreateVersion7(), Name = "Dad Joke", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow };
        var techTag = new Tag
            { Id = Guid.CreateVersion7(), Name = "Tech", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow };
        var punTag = new Tag
            { Id = Guid.CreateVersion7(), Name = "Pun", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow };
        var existentialTag = new Tag
            { Id = Guid.CreateVersion7(), Name = "Existential", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow };
        var darkTag = new Tag
            { Id = Guid.CreateVersion7(), Name = "Dark", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow };
        var societyTag = new Tag
            { Id = Guid.CreateVersion7(), Name = "Society", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow };

        var jokes = new List<Joke>
        {
            // Programming Jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Why do programmers prefer dark mode? Because light attracts bugs!",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ModifiedAt = DateTime.UtcNow.AddDays(-5),
                LaughScore = 85,
                Tags = [programmingTag, techTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "What's a programmer's favorite place in the house? The Arrays (stairs)!",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                ModifiedAt = DateTime.UtcNow.AddDays(-3),
                LaughScore = 72,
                Tags = [programmingTag, punTag]
            },

            // Dad Jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "What do you call a fake noodle? An impasta!",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ModifiedAt = DateTime.UtcNow.AddDays(-2),
                LaughScore = 65,
                Tags = [dadJokeTag, punTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Why don't eggs tell jokes? They'd crack up!",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                ModifiedAt = DateTime.UtcNow.AddDays(-3),
                LaughScore = 58,
                Tags = [dadJokeTag]
            },

            // Self-deprecating Tech Jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "I'm such a bad programmer, I can't even get my life to compile properly.",
                Type = JokeType.SelfDeprecating,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ModifiedAt = DateTime.UtcNow.AddDays(-2),
                LaughScore = 91,
                Tags = [programmingTag, techTag]
            },

            // Funny Sayings
            new()
            {
                Id = Guid.CreateVersion7(),
                Content =
                    "I'm not saying I'm Wonder Woman, I'm just saying no one has ever seen me and Wonder Woman in the same room together.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ModifiedAt = DateTime.UtcNow.AddDays(-1),
                LaughScore = 88,
                Tags = []
            },
            // Bleak observational jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Every time someone says 'life is short,' it gets a little longer.",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                ModifiedAt = DateTime.UtcNow.AddDays(-3),
                LaughScore = 85,
                Tags = [existentialTag, darkTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "The light at the end of the tunnel has been turned off due to budget cuts.",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                ModifiedAt = DateTime.UtcNow.AddDays(-28),
                LaughScore = 92,
                Tags = [darkTag, societyTag]
            },

            // Dark sayings
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Life is like a box of chocolates - mostly disappointing and gone too fast.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                ModifiedAt = DateTime.UtcNow.AddDays(-24),
                LaughScore = 88,
                Tags = [darkTag, existentialTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Think of how stupid the average person is, then realize half of them are stupider than that.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                ModifiedAt = DateTime.UtcNow.AddDays(-20),
                LaughScore = 91,
                Tags = [societyTag, darkTag]
            },

            // Discouraging truths
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Your entire life has been leading up to this moment. This moment also kind of sucks.",
                Type = JokeType.Discouragement,
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                ModifiedAt = DateTime.UtcNow.AddDays(-18),
                LaughScore = 94,
                Tags = [existentialTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "You're not stuck in traffic. You are the traffic. You are the problem.",
                Type = JokeType.Discouragement,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                ModifiedAt = DateTime.UtcNow.AddDays(-15),
                LaughScore = 89,
                Tags = [societyTag, darkTag]
            },

            // Self-deprecating existential humor
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "I used to think I was indecisive. Now I'm not so sure. Actually, maybe I am.",
                Type = JokeType.SelfDeprecating,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                ModifiedAt = DateTime.UtcNow.AddDays(-12),
                LaughScore = 87,
                Tags = [existentialTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "I have a lot of growing up to do. I realized that the other day inside my blanket fort.",
                Type = JokeType.SelfDeprecating,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ModifiedAt = DateTime.UtcNow.AddDays(-10),
                LaughScore = 90,
                Tags = [existentialTag, societyTag]
            },

            // More existential observations
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Nothing like a good night's sleep to remind you you're still tired of existence.",
                Type = JokeType.Discouragement,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                ModifiedAt = DateTime.UtcNow.AddDays(-8),
                LaughScore = 93,
                Tags = [existentialTag, darkTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "The more people I meet, the more I enjoy social distancing.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ModifiedAt = DateTime.UtcNow.AddDays(-5),
                LaughScore = 95,
                Tags = [societyTag, darkTag]
            }
        };

        // Add all jokes to the context
        context.Jokes.AddRange(jokes);

        // Save changes to persist the seed data
        await context.SaveChangesAsync();

        Console.WriteLine($"Database seeded with {jokes.Count} jokes");
    }

    public static async Task<List<Joke>> SeedDataAsync()
    {
        // Create some common tags that can be reused across jokes
        var programmingTag = new Tag { Id = Guid.CreateVersion7(), Name = "Programming" };
        var dadJokeTag = new Tag { Id = Guid.CreateVersion7(), Name = "Dad Joke" };
        var techTag = new Tag { Id = Guid.CreateVersion7(), Name = "Tech" };
        var punTag = new Tag { Id = Guid.CreateVersion7(), Name = "Pun" };
        var existentialTag = new Tag { Id = Guid.CreateVersion7(), Name = "Existential" };
        var darkTag = new Tag { Id = Guid.CreateVersion7(), Name = "Dark" };
        var societyTag = new Tag { Id = Guid.CreateVersion7(), Name = "Society" };

        var jokes = new List<Joke>
        {
            // Programming Jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Why do programmers prefer dark mode? Because light attracts bugs!",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                LaughScore = 85,
                Tags = [programmingTag, techTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "What's a programmer's favorite place in the house? The Arrays (stairs)!",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                LaughScore = 72,
                Tags = [programmingTag, punTag]
            },

            // Dad Jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "What do you call a fake noodle? An impasta!",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                LaughScore = 65,
                Tags = [dadJokeTag, punTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Why don't eggs tell jokes? They'd crack up!",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                LaughScore = 58,
                Tags = [dadJokeTag]
            },

            // Self-deprecating Tech Jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "I'm such a bad programmer, I can't even get my life to compile properly.",
                Type = JokeType.SelfDeprecating,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                LaughScore = 91,
                Tags = [programmingTag, techTag]
            },

            // Funny Sayings
            new()
            {
                Id = Guid.CreateVersion7(),
                Content =
                    "I'm not saying I'm Wonder Woman, I'm just saying no one has ever seen me and Wonder Woman in the same room together.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LaughScore = 88,
                Tags = []
            },
            // Bleak observational jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Every time someone says 'life is short,' it gets a little longer.",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LaughScore = 85,
                Tags = [existentialTag, darkTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "The light at the end of the tunnel has been turned off due to budget cuts.",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                LaughScore = 92,
                Tags = [darkTag, societyTag]
            },

            // Dark sayings
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Life is like a box of chocolates - mostly disappointing and gone too fast.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                LaughScore = 88,
                Tags = [darkTag, existentialTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Think of how stupid the average person is, then realize half of them are stupider than that.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                LaughScore = 91,
                Tags = [societyTag, darkTag]
            },

            // Discouraging truths
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Your entire life has been leading up to this moment. This moment also kind of sucks.",
                Type = JokeType.Discouragement,
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                LaughScore = 94,
                Tags = [existentialTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "You're not stuck in traffic. You are the traffic. You are the problem.",
                Type = JokeType.Discouragement,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                LaughScore = 89,
                Tags = [societyTag, darkTag]
            },

            // Self-deprecating existential humor
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "I used to think I was indecisive. Now I'm not so sure. Actually, maybe I am.",
                Type = JokeType.SelfDeprecating,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                LaughScore = 87,
                Tags = [existentialTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "I have a lot of growing up to do. I realized that the other day inside my blanket fort.",
                Type = JokeType.SelfDeprecating,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                LaughScore = 90,
                Tags = [existentialTag, societyTag]
            },

            // More existential observations
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Nothing like a good night's sleep to remind you you're still tired of existence.",
                Type = JokeType.Discouragement,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                LaughScore = 93,
                Tags = [existentialTag, darkTag]
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "The more people I meet, the more I enjoy social distancing.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                LaughScore = 95,
                Tags = [societyTag, darkTag]
            }
        };
        Console.WriteLine($"Database async seeded with {jokes.Count} jokes");

        return jokes;
    }
}