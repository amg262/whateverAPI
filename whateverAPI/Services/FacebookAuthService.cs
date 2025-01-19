using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using whateverAPI.Models;
using whateverAPI.Options;

namespace whateverAPI.Services;

public interface IFacebookAuthService : IOAuthService
{
}

/// <summary>
/// Provides Facebook OAuth 2.0 authentication services, handling the complete OAuth flow
/// from authorization URL generation to token exchange and user information retrieval.
/// </summary>
public class FacebookAuthService : IFacebookAuthService
{
    private readonly HttpClient _httpClient;
    private readonly FacebookOptions _settings;
    private readonly ILogger<FacebookAuthService> _logger;

    public FacebookAuthService(
        HttpClient httpClient,
        IOptions<FacebookOptions> settings,
        ILogger<FacebookAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string GenerateOAuthUrl()
    {
        var scopes = new[]
        {
            "email",
            "public_profile"
        };

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _settings.AppId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", scopes),
            ["state"] = Guid.NewGuid().ToString()
        };

        var queryString = string.Join("&", queryParams.Select(p =>
            $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

        return $"https://www.facebook.com/v18.0/dialog/oauth?{queryString}";
    }

    public async Task<TokenResponse> ExchangeCodeForTokensAsync(string code)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = _settings.AppId,
            ["client_secret"] = _settings.AppSecret,
            ["code"] = code,
            ["redirect_uri"] = _settings.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await _httpClient.PostAsync(
            "https://graph.facebook.com/v18.0/oauth/access_token",
            new FormUrlEncodedContent(tokenRequest));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var error = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError("Token exchange failed: {Error}", error);
            throw new HttpRequestException("Failed to exchange code for tokens");
        }

        var response = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        return response ?? throw new HttpRequestException("Invalid token response");
    }

    public async Task<TResponse> GetUserInfoAsync<TResponse>(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            // Request fields we want to retrieve from Facebook
            var fields = "id,name,email,picture";
            var response = await _httpClient.GetAsync(
                $"https://graph.facebook.com/v18.0/me?fields={fields}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get user info: {Error}", error);
                throw new HttpRequestException("Failed to get user information");
            }

            var userInfo = await response.Content.ReadFromJsonAsync<TResponse>();
            return userInfo ?? throw new HttpRequestException("Invalid user info response");
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<TResponse> HandleCallbackAsync<TResponse>(string code)
    {
        var tokenResponse = await ExchangeCodeForTokensAsync(code);
        var userInfo = await GetUserInfoAsync<TResponse>(tokenResponse.AccessToken);
        return userInfo;
    }
}