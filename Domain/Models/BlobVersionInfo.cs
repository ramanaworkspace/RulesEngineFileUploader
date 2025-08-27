namespace Domain.Models;

public sealed class BlobVersionInfo
{
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset? LastModified { get; init; }
    public long? SizeBytes { get; init; }
    public IDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    public string Url { get; init; } = string.Empty;
}
