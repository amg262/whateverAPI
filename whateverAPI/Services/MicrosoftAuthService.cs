using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using whateverAPI.Models;
using whateverAPI.Options;

namespace whateverAPI.Services;

/// <summary>
/// Provides Microsoft OAuth 2.0 authentication services, handling the complete OAuth flow
/// from authorization URL generation to token exchange and user information retrieval.
/// </summary>
public class MicrosoftAuthService
{
    private readonly HttpClient _httpClient;
    private readonly MicrosoftOptions _settings;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<MicrosoftAuthService> _logger;

    public MicrosoftAuthService(
        HttpClient httpClient,
        IOptions<MicrosoftOptions> settings,
        IJwtTokenService jwtTokenService,
        ILogger<MicrosoftAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public string GenerateMicrosoftOAuthUrl()
    {
        var scopes = new[]
        {
            "openid",
            "email",
            "profile",
            "User.Read"
        };

        // Ensure all dictionary values are non-null strings
        var queryParams = new Dictionary<string, string>
        {
            // Use ClientId as client_id for Microsoft OAuth
            ["client_id"] = _settings.ClientId ?? throw new InvalidOperationException("ClientId is not configured"),
            ["redirect_uri"] = _settings.RedirectUri ?? throw new InvalidOperationException("RedirectUri is not configured"),
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", scopes),
            ["response_mode"] = "query",
            // Add state parameter for security
            ["state"] = Guid.NewGuid().ToString()
        };

        // Build query string with null checking
        var queryString = string.Join("&", queryParams
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        return $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?{queryString}";
    }

    /// <summary>
    /// Processes the OAuth callback by exchanging the authorization code for tokens
    /// and retrieving the user's information from Microsoft.
    /// </summary>
    public async Task<MicrosoftUserInfo> HandleMicrosoftCallback(string code)
    {
        var tokenResponse = await ExchangeCodeForTokens(code);
        return await GetUserInfo(tokenResponse.AccessToken);
    }

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens.
    /// </summary>
    private async Task<MicrosoftTokenResponse> ExchangeCodeForTokens(string code)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["redirect_uri"] = _settings.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await _httpClient.PostAsync(
            "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            new FormUrlEncodedContent(tokenRequest));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var error = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError("Token exchange failed: {Error}", error);
            throw new HttpRequestException("Failed to exchange code for tokens");
        }

        var response = await tokenResponse.Content.ReadFromJsonAsync<MicrosoftTokenResponse>();
        return response ?? throw new HttpRequestException("Invalid token response");
    }

    /// <summary>
    /// Retrieves user information from Microsoft's Graph API using an access token.
    /// </summary>
    private async Task<MicrosoftUserInfo> GetUserInfo(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            // Microsoft Graph API endpoint for user profile
            var response = await _httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get user info: {Error}", error);
                throw new HttpRequestException("Failed to get user information");
            }

            var userInfo = await response.Content.ReadFromJsonAsync<MicrosoftUserInfo>();
            return userInfo ?? throw new HttpRequestException("Invalid user info response");
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}


