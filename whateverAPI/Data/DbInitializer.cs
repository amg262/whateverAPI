using Microsoft.EntityFrameworkCore;
using Polly;
using whateverAPI.Entities;
using whateverAPI.Helpers;

namespace whateverAPI.Data;

/// <summary>
/// Provides database initialization and seeding functionality with robust error handling and retry logic,
/// particularly designed for containerized environments where database availability might be delayed.
/// </summary>
/// <remarks>
/// This static class implements a comprehensive database initialization strategy that addresses several
/// critical aspects of database management in modern applications:
/// 
/// Infrastructure Considerations:
/// - Handles delayed database availability in containerized environments
/// - Implements exponential backoff for connection retries
/// - Provides detailed logging of initialization attempts
/// - Supports proper error handling and recovery
/// 
/// Data Management:
/// - Ensures database schema is up to date through migrations
/// - Implements idempotent seeding operations
/// - Maintains data consistency through careful initialization
/// - Provides diverse sample data for testing
/// 
/// The initialization process is designed to be resilient and self-healing,
/// making it particularly suitable for cloud deployments and container orchestration
/// environments where service availability might be initially unstable.
/// </remarks>
public static class DbInitializer
{
    /// <summary>
    /// Initializes the database with an intelligent retry strategy using exponential backoff,
    /// making it resilient to temporary connection issues in containerized environments.
    /// </summary>
    /// <param name="app">The web application instance to configure</param>
    /// <param name="maxRetryAttempts">Maximum number of connection retry attempts</param>
    /// <param name="maxDelaySeconds">Maximum delay between retries in seconds</param>
    /// <returns>The web application instance for method chaining</returns>
    /// <remarks>
    /// This method implements a sophisticated retry strategy that handles common startup scenarios:
    /// 
    /// Retry Implementation:
    /// - Uses exponential backoff to avoid overwhelming the database
    /// - Implements maximum delay cap to prevent excessive waiting
    /// - Provides detailed logging of retry attempts
    /// - Handles various database connection exceptions
    /// 
    /// The method specifically handles these scenarios:
    /// - Database container still starting up
    /// - Network connectivity issues
    /// - Temporary database unavailability
    /// - Connection timeouts
    /// </remarks>
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
            await Seed(app, context);
        });
    }

    /// <summary>
    /// Performs the core database initialization process, including migration application
    /// and data seeding operations.
    /// </summary>
    /// <param name="app">The web application instance</param>
    /// <param name="context"></param>
    /// <remarks>
    /// The method is designed to be idempotent, meaning it can be safely
    /// executed multiple times without causing data duplication or inconsistency.
    /// </remarks>
    private static async Task Seed(WebApplication app, AppDbContext context)
    {
        // Check if database is already seeded
        if (await context.Roles.AnyAsync())
        {
            app.Logger.LogInformation("Database already contains data - skipping seed");
            return;
        }

        // Create roles first
        var adminRole = Role.Create(Helper.AdminRole, "Full system access");
        var modRole = Role.Create(Helper.ModeratorRole, "Content management access");
        var userRole = Role.Create(Helper.UserRole, "Basic user access");

        context.Roles.AddRange(adminRole, modRole, userRole);
        await context.SaveChangesAsync();

        // Create users with their roles
        var adminUser = User.Create(Helper.AdminRole, $"{Helper.AdminRole}@{Helper.AdminRole}.com", adminRole.Id);
        var modUser = User.Create(Helper.ModeratorRole, $"{Helper.ModeratorRole}@{Helper.ModeratorRole}.com", modRole.Id);
        var normalUser = User.Create(Helper.UserRole, $"{Helper.UserRole}@{Helper.UserRole}.com", userRole.Id);

        context.Users.AddRange(adminUser, modUser, normalUser);
        await context.SaveChangesAsync();

        // Create tags
        var darkTag = Tag.Create("Dark");
        var existentialTag = Tag.Create("Existential");

        context.Tags.AddRange(darkTag, existentialTag);
        await context.SaveChangesAsync();

        var users = context.Users.ToList();
        var random = new Random();

        // Simple function to get a random user ID

        // Create jokes with random user assignments
        var jokes = new List<Joke>
        {
            // Bleak observational jokes
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Every time someone says 'life is short,' it gets a little longer.",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                ModifiedAt = DateTime.UtcNow.AddDays(-3),
                LaughScore = 85,
                Tags = [existentialTag, darkTag],
                UserId = GetRandomUserId(users, random)
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "The light at the end of the tunnel has been turned off due to budget cuts.",
                Type = JokeType.Joke,
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                ModifiedAt = DateTime.UtcNow.AddDays(-28),
                LaughScore = 92,
                Tags = [darkTag],
                UserId = GetRandomUserId(users, random)
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
                Tags = [darkTag, existentialTag],
                UserId = GetRandomUserId(users, random)
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "Think of how stupid the average person is, then realize half of them are stupider than that.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                ModifiedAt = DateTime.UtcNow.AddDays(-20),
                LaughScore = 91,
                Tags = [darkTag],
                UserId = GetRandomUserId(users, random)
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
                Tags = [existentialTag],
                UserId = GetRandomUserId(users, random)
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "You're not stuck in traffic. You are the traffic. You are the problem.",
                Type = JokeType.Discouragement,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                ModifiedAt = DateTime.UtcNow.AddDays(-15),
                LaughScore = 89,
                Tags = [darkTag],
                UserId = GetRandomUserId(users, random)
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
                Tags = [existentialTag],
                UserId = GetRandomUserId(users, random)
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "I have a lot of growing up to do. I realized that the other day inside my blanket fort.",
                Type = JokeType.SelfDeprecating,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ModifiedAt = DateTime.UtcNow.AddDays(-10),
                LaughScore = 90,
                Tags = [existentialTag],
                UserId = GetRandomUserId(users, random)
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
                Tags = [existentialTag, darkTag],
                UserId = GetRandomUserId(users, random)
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Content = "The more people I meet, the more I enjoy social distancing.",
                Type = JokeType.FunnySaying,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ModifiedAt = DateTime.UtcNow.AddDays(-5),
                LaughScore = 95,
                Tags = [darkTag],
                UserId = GetRandomUserId(users, random)
            }
        };

        context.Jokes.AddRange(jokes);
        await context.SaveChangesAsync();

        app.Logger.LogInformation("Database seeding completed successfully with {JokeCount} jokes", jokes.Count);
    }

    private static Guid GetRandomUserId(List<User> users, Random random) => users[random.Next(users.Count)].Id;
}