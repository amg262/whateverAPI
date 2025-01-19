namespace whateverAPI.Options;

public class FacebookOptions
{
    public required string AppId { get; init; }
    public required string AppSecret { get; init; }
    public required string RedirectUri { get; init; }
}