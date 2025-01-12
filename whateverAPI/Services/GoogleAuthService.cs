using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using whateverAPI.Models;
using whateverAPI.Options;

namespace whateverAPI.Services;

/// <summary>
/// Provides Google OAuth 2.0 authentication services for the application, handling the complete OAuth flow
/// from initial authorization URL generation to token exchange and user information retrieval.
/// </summary>
/// <remarks>
/// This service implements the OAuth 2.0 authorization code flow for Google authentication:
/// 1. Generates the authorization URL with specified scopes (GenerateGoogleOAuthUrl)
/// 2. Exchanges the authorization code for access/refresh tokens (ExchangeCodeForTokens)
/// 3. Retrieves user information using the access token (GetUserInfo)
/// 
/// Required Google OAuth scopes:
/// - openid: OpenID Connect integration
/// - email: User's email address
/// - profile: Basic profile information
/// - https://www.googleapis.com/auth/user.birthday.read: Access to user's birthday
/// - https://www.googleapis.com/auth/user.phonenumbers.read: Access to phone numbers
/// - https://www.googleapis.com/auth/user.addresses.read: Access to addresses
/// </remarks>
public class GoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleOptions _settings;
    private readonly ILogger<GoogleAuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the GoogleAuthService with required dependencies.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests to Google APIs</param>
    /// <param name="settings">Configuration settings for Google OAuth</param>
    /// <param name="logger">Logger for error tracking and debugging</param>
    public GoogleAuthService(
        HttpClient httpClient,
        IOptions<GoogleOptions> settings,
        ILogger<GoogleAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generates the Google OAuth authorization URL with all required scopes and parameters.
    /// </summary>
    /// <returns>
    /// A fully formatted URL string that will redirect users to Google's consent page.
    /// The URL includes client ID, redirect URI, response type, requested scopes,
    /// and additional parameters for offline access and consent prompt.
    /// </returns>
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
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", scopes),
            ["access_type"] = "offline",
            ["prompt"] = "consent"
        };

        var queryString = string.Join("&", queryParams.Select(p =>
            $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

        return $"https://accounts.google.com/o/oauth2/v2/auth?{queryString}";
    }

    /// <summary>
    /// Processes the OAuth callback by exchanging the authorization code for tokens
    /// and retrieving the user's information from Google.
    /// </summary>
    /// <param name="code">The authorization code received from Google's OAuth consent page</param>
    /// <returns>
    /// A GoogleUserInfo object containing the authenticated user's information
    /// retrieved from Google's userinfo endpoint.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when token exchange fails or user information cannot be retrieved
    /// </exception>
    public async Task<GoogleUserInfo> HandleGoogleCallback(string code)
    {
        // First, exchange the code for tokens
        var tokenResponse = await ExchangeCodeForTokens(code);

        // Then, get the user information using the access token
        return await GetUserInfo(tokenResponse.AccessToken);
    }

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens.
    /// </summary>
    /// <param name="code">The authorization code to exchange</param>
    /// <returns>
    /// A GoogleTokenResponse containing access token, refresh token, and related metadata
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the token exchange fails or returns invalid data
    /// </exception>
    private async Task<GoogleTokenResponse> ExchangeCodeForTokens(string code)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["redirect_uri"] = _settings.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await _httpClient
            .PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(tokenRequest));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var error = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError("Token exchange failed: {Error}", error);
            throw new HttpRequestException("Failed to exchange code for tokens");
        }

        var response = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        return response ?? throw new HttpRequestException("Invalid token response");
    }

    /// <summary>
    /// Retrieves user information from Google's userinfo endpoint using an access token.
    /// </summary>
    /// <param name="accessToken">The access token to authenticate the request</param>
    /// <returns>
    /// A GoogleUserInfo object containing the user's profile information
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when user information cannot be retrieved or is invalid
    /// </exception>
    private async Task<GoogleUserInfo> GetUserInfo(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

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