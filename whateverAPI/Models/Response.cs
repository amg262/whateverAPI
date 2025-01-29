using System.Text.Json.Serialization;
using whateverAPI.Entities;

namespace whateverAPI.Models;

public record JokeResponse
{
    public Guid Id { get; init; }
    public string? Content { get; init; }
    public JokeType? Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public List<string>? Tags { get; init; } = [];
    public int? LaughScore { get; init; }
    public bool IsActive { get; init; }
    public string? UserId { get; init; }
    
    public string? UserName { get; init; }
    
    public string? UserEmail { get; init; }

}

public record TagResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public bool IsActive { get; init; }
}

public record JokeApiResponse
{
    [JsonPropertyName("error")] public bool Error { get; set; }

    [JsonPropertyName("joke")] public string Joke { get; set; }

    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;

    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("setup")] public string? Setup { get; set; }

    [JsonPropertyName("delivery")] public string? Delivery { get; set; }

    [JsonPropertyName("flags")] public JokeFlags Flags { get; set; } = new();

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("safe")] public bool Safe { get; set; }

    [JsonPropertyName("lang")] public string Lang { get; set; } = string.Empty;
}

/// <summary>
/// Represents the content flags associated with a joke,
/// indicating various categories of potentially sensitive content.
/// </summary>
public class JokeFlags
{
    [JsonPropertyName("nsfw")] public bool Nsfw { get; set; }

    [JsonPropertyName("religious")] public bool Religious { get; set; }

    [JsonPropertyName("political")] public bool Political { get; set; }

    [JsonPropertyName("racist")] public bool Racist { get; set; }

    [JsonPropertyName("sexist")] public bool Sexist { get; set; }

    [JsonPropertyName("explicit")] public bool Explicit { get; set; }
}

public record GoogleTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";

    [JsonPropertyName("id_token")] public string IdToken { get; init; } = "";

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
}

public record TokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";

    [JsonPropertyName("id_token")] public string IdToken { get; init; } = "";

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
}

public record GoogleUserInfo
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("email")] public string Email { get; init; } = "";

    [JsonPropertyName("verified_email")] public bool EmailVerified { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = "";

    [JsonPropertyName("given_name")] public string? GivenName { get; init; }

    [JsonPropertyName("family_name")] public string? FamilyName { get; init; }
    [JsonPropertyName("picture")] public string? Picture { get; init; }

    [JsonPropertyName("locale")] public string? Locale { get; init; }

    [JsonPropertyName("timezone")] public string? Timezone { get; init; }

    [JsonPropertyName("gender")] public string? Gender { get; init; }

    [JsonPropertyName("birthdate")] public string? Birthdate { get; init; }
}

/// <summary>
/// Represents the response from Microsoft's token endpoint.
/// </summary>
public record MicrosoftTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";

    [JsonPropertyName("id_token")] public string IdToken { get; init; } = "";

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
}

/// <summary>
/// Represents user information retrieved from Microsoft Graph API.
/// </summary>
public record MicrosoftUserInfo
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("displayName")] public string Name { get; init; } = "";
    [JsonPropertyName("userPrincipalName")] public string Email { get; init; } = "";

    [JsonPropertyName("givenName")] public string? GivenName { get; init; }

    [JsonPropertyName("surname")] public string? Surname { get; init; }

    [JsonPropertyName("jobTitle")] public string? JobTitle { get; init; }

    [JsonPropertyName("officeLocation")] public string? OfficeLocation { get; init; }

    [JsonPropertyName("preferredLanguage")] public string? PreferredLanguage { get; init; }
    [JsonPropertyName("picture")] public string? Picture { get; set; }
}

/// <summary>
/// Represents user information retrieved from Facebook Graph API.
/// </summary>
public record FacebookUserInfo
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";

    [JsonPropertyName("name")] public string Name { get; init; } = "";

    [JsonPropertyName("email")] public string Email { get; init; } = "";

    [JsonPropertyName("picture")] public FacebookPicture Picture { get; init; } = new();
}

public record FacebookPicture
{
    [JsonPropertyName("data")] public FacebookPictureData Data { get; init; } = new();
}

public record FacebookPictureData
{
    [JsonPropertyName("url")] public string Url { get; init; } = "";
}