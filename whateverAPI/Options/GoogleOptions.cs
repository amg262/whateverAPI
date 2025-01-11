namespace whateverAPI.Options;

public class GoogleOptions
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string RedirectUri { get; init; }
}