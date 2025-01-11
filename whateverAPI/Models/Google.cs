// Models for handling Google token verification
// First, we need the configuration class

using System.Text.Json.Serialization;

namespace whateverAPI.Models;

// Response models
public record GoogleTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";

    [JsonPropertyName("id_token")] public string IdToken { get; init; } = "";

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
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