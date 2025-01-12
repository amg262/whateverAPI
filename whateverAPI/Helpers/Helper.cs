namespace whateverAPI.Helpers;

public static class Helper
{
    public const string JokeTagsTableName = "JokeTags";
    public const string TokenCookie = "whateverToken";
    public const string CorsPolicy = "CorsPolicy";
    public const string AuthToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1lIjoiYXNkZmFzZGZhc2ZkIiwic3ViIjoiYXNkZmFzZGZAYS" +
        "5jb20iLCJqdGkiOiIwMTk0NGNmOS02MGNjLTcwMWQtOWVjNi05Y2JlNGZiZjViYTAiLCJpYXQiOjE3MzY0NTgxOTksI" +
        "mlwIjoiOjoxIiwibmJmIjoxNzM2NDU4MTk5LCJleHAiOjI1MzQwMjMwMDgwMCwiaXNzIjoiY3Jpc2lzLXByZXZlbnRp" +
        "b24taW5zdGl0dXRlIiwiYXVkIjoiY3BpLXN3ZS1kZXYifQ.tjhEbPrUWBh3d47lk9_FN3owIiQUL_7SA6O05P5Yy7E";
    
    
    /// <summary>
    /// Maps properties from a source object to a destination object based on matching property names.
    /// </summary>
    /// <param name="source">The source object to map from</param>
    /// <param name="destination">The destination object to map to</param>
    /// <typeparam name="T">The type of objects being mapped</typeparam>
    /// <returns>The destination object with mapped properties</returns>
    /// <remarks>
    /// This method:
    /// - Preserves existing values when source properties are null
    /// - Only maps properties with matching names and compatible types
    /// - Handles null checking for both source and destination
    /// </remarks>
    public static T MapProperties<T>(T source, T destination) where T : class
    {
        // Validate parameters
        if (source == null || destination == null)
        {
            throw new ArgumentNullException(
                source == null ? nameof(source) : nameof(destination),
                "Source and destination objects cannot be null");
        }

        // Get properties of the type
        var properties = typeof(T)
            .GetProperties()
            .Where(p => p is { CanRead: true, CanWrite: true });

        // Map each property
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(source);
                // Only update if source value is not null
                if (value != null)
                {
                    prop.SetValue(destination, value);
                }
            }
            catch (Exception ex)
            {
                
                
                
                // Log or handle specific property mapping failures
                // This allows the method to continue with other properties
                // even if one fails
            }
        }

        return destination;
    }
}