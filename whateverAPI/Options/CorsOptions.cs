using whateverAPI.Helpers;

namespace whateverAPI.Options;

public class CorsOptions
{
    public string PolicyName { get; set; } = Helper.DefaultPolicy;
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = [];
    public string[] AllowedHeaders { get; set; } = [];
    public bool AllowCredentials { get; set; } = true;
}