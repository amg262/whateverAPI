﻿using Microsoft.EntityFrameworkCore;
using whateverAPI.Entities;

namespace whateverAPI.Data;

/// <summary>
/// Provides functionality to initialize and seed the database with joke data.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initializes and seeds the joke database at application startup.
    /// Applies any pending migrations and seeds initial data if necessary.
    /// </summary>
    public static async Task InitDb(WebApplication app)
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

        // Add all jokes to the context
        context.Jokes.AddRange(jokes);

        // Save changes to persist the seed data
        await context.SaveChangesAsync();

        Console.WriteLine($"Database seeded with {jokes.Count} jokes");
    }
}