using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using whateverAPI.Models;
using whateverAPI.Options;

namespace whateverAPI.Services;

public class GoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<GoogleOAuthOptions> _settings;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        HttpClient httpClient,
        IOptions<GoogleOAuthOptions> settings,
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

        var response = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        return response ?? throw new HttpRequestException("Invalid token response");
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

            var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
            return userInfo ?? throw new HttpRequestException("Invalid user info response");
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}