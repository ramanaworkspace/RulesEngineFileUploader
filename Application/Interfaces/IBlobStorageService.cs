
using Domain.Models;

namespace Application.Interfaces;

public interface IBlobStorageService
{
    Task<UploadResponse> UploadVersionedAsync(Stream file, string? uploadedBy = null, CancellationToken ct = default);
    Task<IReadOnlyList<BlobVersionInfo>> ListVersionsAsync(CancellationToken ct = default);
    Task<string> GetLatestUrlAsync(CancellationToken ct = default);
    Task<UploadResponse> RollbackAsync(string versionedName, string? requestedBy = null, CancellationToken ct = default);
}
