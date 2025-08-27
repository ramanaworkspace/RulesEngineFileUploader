namespace Domain.Models;

public sealed class UploadResponse
{
    public string StableBlobUrl { get; init; } = string.Empty;
    public string VersionedBlobUrl { get; init; } = string.Empty;
    public string VersionedBlobName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
