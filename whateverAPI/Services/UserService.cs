﻿using Microsoft.EntityFrameworkCore;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;

namespace whateverAPI.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserFromOAuthAsync(OAuthUserInfo userInfo, CancellationToken ct = default)
    {
        // First try to find user by provider-specific ID
        var user = userInfo.Provider?.ToLower() switch
        {
            Helper.GoogleProvider => await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == userInfo.Id, ct),
            Helper.MicrosoftProvider => await _db.Users.FirstOrDefaultAsync(u => u.MicrosoftId == userInfo.Id, ct),
            Helper.FacebookProvider => await _db.Users.FirstOrDefaultAsync(u => u.FacebookId == userInfo.Id, ct),
            _ => null
        };

        // If not found by provider ID, try to find by email
        user ??= await _db.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email.ToLower(), ct);

        if (user != null)
        {
            // Step 3: Update basic user information
            user.Name = userInfo.Name;

            if (!string.IsNullOrEmpty(userInfo.Picture))
            {
                Console.WriteLine("Picture is null");
                user.PictureUrl = userInfo.Picture;
            }

            // user.PictureUrl = userInfo.Picture ?? user.PictureUrl;
            user.ModifiedAt = DateTime.UtcNow;

            // Change it to most recently used provider
            user.Provider = userInfo.Provider;

            // Step 4: Link the new provider ID if it's not already set
            switch (userInfo.Provider?.ToLower())
            {
                case Helper.GoogleProvider when user.GoogleId == null:
                    user.GoogleId = userInfo.Id;
                    _logger.LogInformation("Linked Google account {Id} to existing user {Email}", userInfo.Id, user.Email);
                    break;
                case Helper.MicrosoftProvider when user.MicrosoftId == null:
                    user.MicrosoftId = userInfo.Id;
                    _logger.LogInformation("Linked Microsoft account {Id} to existing user {Email}", userInfo.Id, user.Email);
                    break;
                case Helper.FacebookProvider when user.FacebookId == null:
                    user.FacebookId = userInfo.Id;
                    _logger.LogInformation("Linked Facebook account {Id} to existing user {Email}", userInfo.Id, user.Email);
                    break;
            }

            // Step 5: Update the most recent provider while preserving linked accounts
            _logger.LogInformation(
                "Updated user {Email} via {Provider} login. GoogleId: {GoogleId}, MicrosoftId: {MicrosoftId}, FacebookId: {FacebookId}",
                user.Email, userInfo.Provider, user.GoogleId, user.MicrosoftId, user.FacebookId);

            await _db.SaveChangesAsync(ct);
            return user;
        }

        // Create new user if not found
        var newUser = User.FromOAuthInfo(userInfo);
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created new user: {Email} via {Provider}", newUser.Email, userInfo.Provider);
        return newUser;
    }

    public async Task<User?> GetUserById(Guid id, CancellationToken ct = default)
    {
        return await _db.Users
            .Include(u => u.Jokes)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<List<Joke>> GetUserJokes(Guid userId, CancellationToken ct = default)
    {
        return await _db.Jokes
            .Where(j => j.Id == userId)
            .Include(j => j.Tags)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
    }
}