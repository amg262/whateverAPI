// Models for handling Google token verification
// First, we need the configuration class

using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

public class GoogleOAuthSettings
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string RedirectUri { get; init; }
}

public class GoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<GoogleOAuthSettings> _settings;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        HttpClient httpClient,
        IOptions<GoogleOAuthSettings> settings,
        ILogger<GoogleAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    // Generate the Google OAuth URL
    public string GenerateGoogleOAuthUrl()
    {
        var scopes = new[]
        {
            "openid",
            "email",
            "profile",
            "https://www.googleapis.com/auth/user.birthday.read",
            "https://www.googleapis.com/auth/user.phonenumbers.read",
            "https://www.googleapis.com/auth/user.addresses.read"
        };

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _settings.Value.ClientId,
            ["redirect_uri"] = _settings.Value.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", scopes),
            ["access_type"] = "offline",
            ["prompt"] = "consent"
        };

        var queryString = string.Join("&", queryParams.Select(p =>
            $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

        return $"https://accounts.google.com/o/oauth2/v2/auth?{queryString}";
    }

    // Exchange the code for tokens and user information
    public async Task<GoogleUserInfo> HandleGoogleCallback(string code)
    {
        // First, exchange the code for tokens
        var tokenResponse = await ExchangeCodeForTokens(code);

        // Then, get the user information using the access token
        return await GetUserInfo(tokenResponse.AccessToken);
    }

    private async Task<GoogleTokenResponse> ExchangeCodeForTokens(string code)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _settings.Value.ClientId,
            ["client_secret"] = _settings.Value.ClientSecret,
            ["redirect_uri"] = _settings.Value.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var error = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError("Token exchange failed: {Error}", error);
            throw new HttpRequestException("Failed to exchange code for tokens");
        }

        return await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>()
               ?? throw new HttpRequestException("Invalid token response");
    }

    private async Task<GoogleUserInfo> GetUserInfo(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.GetAsync(
                "https://www.googleapis.com/oauth2/v2/userinfo");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get user info: {Error}", error);
                throw new HttpRequestException("Failed to get user information");
            }

            return await response.Content.ReadFromJsonAsync<GoogleUserInfo>()
                   ?? throw new HttpRequestException("Invalid user info response");
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}

// Response models
public class GoogleTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";

    [JsonPropertyName("id_token")] public string IdToken { get; init; } = "";

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
}

public class GoogleUserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("email")]
    public string Email { get; init; } = "";

    [JsonPropertyName("verified_email")]
    public bool EmailVerified { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("given_name")]
    public string? GivenName { get; init; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; init; }

    [JsonPropertyName("picture")]
    public string? Picture { get; init; }

    [JsonPropertyName("locale")]
    public string? Locale { get; init; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    [JsonPropertyName("birthdate")]
    public string? Birthdate { get; init; }
}
// // First, let's create our configuration model for Google OAuth settings
//
// using System.Net.Http.Headers;
// using System.Text.Json.Serialization;
// using Microsoft.Extensions.Caching.Memory;
// using Microsoft.Extensions.Options;
// using whateverAPI.Services;
//
// public class GoogleOAuthOptions
// {
//     public required string ClientId { get; init; }
//     public required string ClientSecret { get; init; }
//     public required string RedirectUri { get; init; }
//     // Using constants for Google's OAuth endpoints
//     public const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
//     public const string TokenEndpoint = "https://oauth2.googleapis.com/token";
//     public const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
// }
//
// // Models to handle OAuth responses
// public record GoogleTokenResponse
// {
//     [JsonPropertyName("access_token")]
//     public required string AccessToken { get; init; }
//
//     [JsonPropertyName("expires_in")]
//     public int ExpiresIn { get; init; }
//
//     [JsonPropertyName("refresh_token")]
//     public string? RefreshToken { get; init; }
//
//     [JsonPropertyName("id_token")]
//     public string? IdToken { get; init; }
// }
//
// public record GoogleUserInfo
// {
//     [JsonPropertyName("id")]
//     public required string Id { get; init; }
//
//     [JsonPropertyName("email")]
//     public required string Email { get; init; }
//
//     [JsonPropertyName("verified_email")]
//     public bool VerifiedEmail { get; init; }
//
//     [JsonPropertyName("name")]
//     public required string Name { get; init; }
//
//     [JsonPropertyName("picture")]
//     public string? Picture { get; init; }
// }
//
// // Service to handle Google OAuth operations
// public class GoogleAuthService
// {
//     private readonly HttpClient _httpClient;
//     private readonly IOptions<GoogleOAuthOptions> _options;
//     private readonly ILogger<GoogleAuthService> _logger;
//
//     public GoogleAuthService(
//         HttpClient httpClient,
//         IOptions<GoogleOAuthOptions> options,
//         ILogger<GoogleAuthService> logger)
//     {
//         _httpClient = httpClient;
//         _options = options;
//         _logger = logger;
//     }
//
//     public string GenerateAuthUrl(string state)
//     {
//         // Build the authorization URL with all required parameters
//         var queryParams = new Dictionary<string, string>
//         {
//             ["client_id"] = _options.Value.ClientId,
//             ["redirect_uri"] = _options.Value.RedirectUri,
//             ["response_type"] = "token",
//             ["scope"] = "openid email profile",
//             ["access_type"] = "offline", // Request refresh token
//             ["state"] = state,
//             ["prompt"] = "consent" // Force consent screen to get refresh token
//         };
//
//         var queryString = string.Join("&", queryParams.Select(p => 
//             $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
//
//         return $"{GoogleOAuthOptions.AuthorizationEndpoint}?{queryString}";
//     }
//
//     public async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(
//         string code, 
//         CancellationToken ct = default)
//     {
//         var tokenRequest = new Dictionary<string, string>
//         {
//             ["code"] = code,
//             ["client_id"] = _options.Value.ClientId,
//             ["client_secret"] = _options.Value.ClientSecret,
//             ["redirect_uri"] = _options.Value.RedirectUri,
//             ["grant_type"] = "authorization_code"
//         };
//
//         using var request = new HttpRequestMessage(HttpMethod.Post, GoogleOAuthOptions.TokenEndpoint)
//         {
//             Content = new FormUrlEncodedContent(tokenRequest)
//         };
//
//         using var response = await _httpClient.SendAsync(request, ct);
//         
//         if (!response.IsSuccessStatusCode)
//         {
//             var error = await response.Content.ReadAsStringAsync(ct);
//             _logger.LogError("Token exchange failed: {Error}", error);
//             throw new HttpRequestException("Failed to exchange code for tokens");
//         }
//
//         return await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct) 
//                ?? throw new HttpRequestException("Invalid token response");
//     }
//
//     public async Task<GoogleUserInfo> GetUserInfoAsync(
//         string accessToken, 
//         CancellationToken ct = default)
//     {
//         _httpClient.DefaultRequestHeaders.Authorization = 
//             new AuthenticationHeaderValue("Bearer", accessToken);
//
//         try
//         {
//             var response = await _httpClient.GetAsync(GoogleOAuthOptions.UserInfoEndpoint, ct);
//
//             if (!response.IsSuccessStatusCode)
//             {
//                 var error = await response.Content.ReadAsStringAsync(ct);
//                 _logger.LogError("Failed to get user info: {Error}", error);
//                 throw new HttpRequestException("Failed to get user information");
//             }
//
//             return await response.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken: ct) 
//                    ?? throw new HttpRequestException("Invalid user info response");
//         }
//         finally
//         {
//             _httpClient.DefaultRequestHeaders.Authorization = null;
//         }
//     }
// }