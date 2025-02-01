using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Services;

namespace whateverAPI.Entities;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    public required string Name { get; set; }
    public required string Email { get; set; }

    // OAuth provider-specific identifiers
    public string? GoogleId { get; set; }
    public string? MicrosoftId { get; set; }

    public string? FacebookId { get; set; }

    // Profile information
    public string? PictureUrl { get; set; }
    public string? Provider { get; set; } // "google", "microsoft", etc.


    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    // User status
    public bool IsActive { get; set; } = true;

    // Navigation property for user's jokes
    [JsonIgnore] public List<Joke> Jokes { get; set; } = [];

    public Guid? RoleId { get; set; }

    [ForeignKey(nameof(RoleId))] public Role? Role { get; set; }

    public static User Create(string name, string email, Guid? roleId = null) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name.Trim(),
        Email = email.ToLower().Trim(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        RoleId = roleId,
        IsActive = true,
    };
    
    // Factory method for creating users from OAuth info
    public static User FromOAuthInfo(OAuthUserInfo userInfo) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = userInfo.Name.Trim(),
        Email = userInfo.Email.ToLower().Trim(),
        PictureUrl = userInfo.Picture,
        Provider = userInfo.Provider,
        GoogleId = userInfo.Provider.Equals(Helper.GoogleProvider, StringComparison.CurrentCultureIgnoreCase)
            ? userInfo.Id
            : null,
        MicrosoftId = userInfo.Provider.Equals(Helper.MicrosoftProvider, StringComparison.CurrentCultureIgnoreCase)
            ? userInfo.Id
            : null,
        FacebookId = userInfo.Provider.Equals(Helper.FacebookProvider, StringComparison.CurrentCultureIgnoreCase)
            ? userInfo.Id
            : null,
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = DateTime.UtcNow,
        IsActive = true,
    };
    
    public static UserAuthor ToUserAuthor(User? user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
    };
}