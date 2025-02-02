namespace whateverAPI.Helpers;

public static class Helper
{
    public const string JokeTagsTableName = "JokeTags";

    public const string DefaultConnection = "DefaultConnection";
    public const string ProductionConnection = "ProductionConnection";

    public const string TokenCookie = "whateverToken";
    public const string DefaultPolicy = "DefaultPolicy";
    
    public const string RateLimiting= "RateLimiting";
    public const string GlobalPolicy = "GlobalRateLimit";
    public const string TokenPolicy = "TokenRateLimit";
    public const string AuthPolicy = "AuthRateLimit";
    public const string ConcurrencyPolicy = "ConcurrencyLimit";

    public const string AuthToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIwMTk0YTRiOS0yYWYzLTc0N2ItOGRkZi0zZWZjZjA1MDk5ZmYiLCJuYW1lIjo" +
        "iYUBhLmNvbSIsImVtYWlsIjoiYW5keSIsImp0aSI6IjAxOTRhNGI5LTQxNjItNzRmMi04OTRkLWIwZjEzNDc4YmU1YiIsImlhdCI6MTczNzk" +
        "zMDQyMSwiaXAiOiI6OjEiLCJwcm92aWRlciI6ImxvY2FsIiwicm9sZSI6InVzZXIiLCJuYmYiOjE3Mzc5MzA0MjEsImV4cCI6MjUzNDAyMzA" +
        "wODAwLCJpc3MiOiJjcmlzaXMtcHJldmVudGlvbi1pbnN0aXR1dGUiLCJhdWQiOiJjcGktc3dlLWRldiJ9.-zqFM-H_-LlNcjVcOQ-UwblSua" +
        "7cFfjsT3JQm62kxCM";

    public const string MicrosoftProvider = "microsoft";
    public const string GoogleProvider = "google";
    public const string FacebookProvider = "facebook";
    
    public const string AdminRole = "admin";
    public const string ModeratorRole = "moderator";
    public const string UserRole = "user";

    public const string RequireAdmin = "RequireAdmin";
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";
    public const string RequireModeratorOrAbove = "RequireModeratorOrAbove";
}