namespace whateverAPI.Options;

public class AzureStorageOptions
{
    public required string ConnectionString { get; init; }
    public required string ContainerName { get; init; }
    public required string BlobStorageUrl { get; init; }
}