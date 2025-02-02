namespace whateverAPI.Options;

public class RateLimitingOptions
{
    // Global rate limit settings
    public int GlobalPermitLimit { get; set; } = 100;
    public int GlobalWindowInSeconds { get; set; } = 60;
    public int GlobalQueueLimit { get; set; } = 2;
    
    // API endpoint specific settings
    public int TokenPermitLimit { get; set; } = 10;
    public int TokenWindowInSeconds { get; set; } = 60;
    public int TokenQueueLimit { get; set; } = 2;

    // Auth endpoint specific settings
    public int AuthPermitLimit { get; set; } = 5;
    public int AuthWindowInSeconds { get; set; } = 60;
    public int AuthQueueLimit { get; set; } = 2;

    // User-specific concurrency limit
    public int ConcurrentRequestLimit { get; set; } = 3;
}