namespace whateverAPI.Options;

public class FrontendOptions
{
    public required string BaseUrl { get; init; }
    public string CallbackPath { get; init; } = "/auth/callback";
    
    public string GetCallbackUrl() => $"{BaseUrl.TrimEnd('/')}{CallbackPath}";
} 