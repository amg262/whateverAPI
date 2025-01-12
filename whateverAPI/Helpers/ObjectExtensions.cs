namespace whateverAPI.Helpers;

/// <summary>
/// Provides extension methods for updating object properties from another instance.
/// </summary>
/// <remarks>
/// This static class adds updating capabilities to any reference type, allowing
/// objects to be easily updated from other instances while maintaining property
/// type safety and null checking.
/// </remarks>
public static class ObjectExtensions
{
    /// <summary>
    /// Updates the current object's properties with non-null values from another instance.
    /// </summary>
    /// <typeparam name="T">The type of objects being updated</typeparam>
    /// <param name="destination">The object being updated (this instance)</param>
    /// <param name="source">The object containing the new values</param>
    /// <returns>The updated destination object for method chaining</returns>
    /// <remarks>
    /// This method implements a safe update strategy:
    /// - Only updates properties when source values are non-null
    /// - Preserves existing values for null source properties
    /// - Handles type compatibility checking
    /// - Supports method chaining for fluent syntax
    /// </remarks>
    public static T MapObject<T>(this T destination, T source) where T : class
    {
        // Validate our parameters
        if (destination == null)
            throw new ArgumentNullException(nameof(destination), "Destination object cannot be null");
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Source object cannot be null");

        // Get all readable properties that can be written to
        var properties = typeof(T).GetProperties()
            .Where(p => p is { CanRead: true, CanWrite: true });

        foreach (var prop in properties)
        {
            try
            {
                // Get the value from the source object
                var value = prop.GetValue(source);

                // Only update if we have a non-null value
                if (value != null)
                {
                    prop.SetValue(destination, value);
                }
            }
            catch (Exception ex)
            {
                // Consider logging the error but continue with other properties
                // We might want to add logging here depending on requirements
            }
        }

        // Return the destination for method chaining
        return destination;
    }

    /// <summary>
    /// Updates the current object's properties from a different type of object with matching property names.
    /// </summary>
    /// <remarks>
    /// This overload allows updating from different types that share property names,
    /// useful when working with DTOs or similar patterns.
    /// </remarks>
    public static TDestination MapObject<TSource, TDestination>(
        this TDestination destination,
        TSource source)
        where TSource : class
        where TDestination : class
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination), "Destination object cannot be null");
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Source object cannot be null");

        // Create a dictionary of destination properties for efficient lookup
        var destProps = typeof(TDestination).GetProperties()
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name);

        // Get all readable properties from source
        var sourceProps = typeof(TSource).GetProperties()
            .Where(p => p.CanRead);

        foreach (var sourceProp in sourceProps)
        {
            // Try to find matching destination property
            if (!destProps.TryGetValue(sourceProp.Name, out var destProp))
                continue;

            // Ensure property types are compatible
            if (!destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                continue;

            try
            {
                var value = sourceProp.GetValue(source);
                if (value != null)
                {
                    destProp.SetValue(destination, value);
                }
            }
            catch (Exception ex)
            {
                // Consider logging the error but continue with other properties
            }
        }

        return destination;
    }
}