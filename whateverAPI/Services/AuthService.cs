using whateverAPI.Helpers;
using whateverAPI.Models;

namespace whateverAPI.Services;

public interface IOAuthService
{
    string GenerateOAuthUrl();

    Task<TokenResponse> ExchangeCodeForTokensAsync(string code);

    Task<TResponse> GetUserInfoAsync<TResponse>(string accessToken);

    Task<TResponse> HandleCallbackAsync<TResponse>(string code);
}

public record OAuthUserInfo
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public string? Picture { get; init; }
    public required string Provider { get; init; }


    public static OAuthUserInfo? FromUserInfoAsync<T>(T userInfo) where T : class, new() => userInfo switch
    {
        MicrosoftUserInfo microsoftUserInfo => FromMicrosoftUserInfo(microsoftUserInfo),
        GoogleUserInfo googleUserInfo => FromGoogleUserInfo(googleUserInfo),
        FacebookUserInfo facebookUserInfo => FromFacebookUserInfo(facebookUserInfo),
        _ => null
    };


    private static OAuthUserInfo FromMicrosoftUserInfo(MicrosoftUserInfo userInfo) => new()
    {
        Id = userInfo.Id,
        Email = userInfo.Email,
        Name = userInfo.Name,
        Picture = userInfo.Picture ?? string.Empty,
        Provider = Helper.MicrosoftProvider,
    };

    private static OAuthUserInfo FromGoogleUserInfo(GoogleUserInfo userInfo) => new()
    {
        Id = userInfo.Id,
        Email = userInfo.Email,
        Name = userInfo.Name,
        Picture = userInfo.Picture ?? string.Empty,
        Provider = Helper.GoogleProvider,
    };

    private static OAuthUserInfo FromFacebookUserInfo(FacebookUserInfo userInfo) => new()
    {
        Id = userInfo.Id,
        Email = userInfo.Email,
        Name = userInfo.Name,
        Picture = userInfo.Picture?.Data?.Url ?? string.Empty,
        Provider = Helper.FacebookProvider,
    };

    public static MicrosoftUserInfo ToMicrosoftUserInfo(OAuthUserInfo userInfo) => new()
    {
        Id = userInfo.Id,
        Email = userInfo.Email,
        Name = userInfo.Name,
        Picture = userInfo.Picture,
    };

    public static GoogleUserInfo ToGoogleUserInfo(OAuthUserInfo userInfo) => new()
    {
        Id = userInfo.Id,
        Email = userInfo.Email,
        Name = userInfo.Name,
        Picture = userInfo.Picture,
    };

    public static FacebookUserInfo ToFacebookUserInfo(OAuthUserInfo userInfo) => new()
    {
        Id = userInfo.Id,
        Email = userInfo.Email,
        Name = userInfo.Name,
        Picture = new FacebookPicture
        {
            Data = new FacebookPictureData
            {
                Url = userInfo.Picture ?? string.Empty
            }
        }
    };
}