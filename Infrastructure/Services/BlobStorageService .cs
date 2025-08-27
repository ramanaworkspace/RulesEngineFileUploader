using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Application.Interfaces;
using Domain.Models;
using Polly.Wrap;
using Polly;
using Polly.Retry;

namespace Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly AzureStorageSettings _settings;
    private readonly BlobContainerClient _container;
    private readonly AsyncPolicyWrap _policy;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IOptions<AzureStorageSettings> options, ILogger<BlobStorageService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        var cred = new DefaultAzureCredential();
        var serviceClient = new BlobServiceClient(new Uri(_settings.AccountUrl), cred);
        _container = serviceClient.GetBlobContainerClient(_settings.ContainerName);
        _container.CreateIfNotExists(PublicAccessType.None);
        _policy = PollyPolicyFactory.CreateDefaultPolicy(logger);
    }

    private static string BuildVersionedName(string stableNameUtc)
    {
        var ts = DateTimeOffset.UtcNow;
        var stamp = ts.ToString("yyyy-MM-dd'T'HH-mm-ss'Z'");
        var prefix = Path.GetFileNameWithoutExtension(stableNameUtc);
        var ext = Path.GetExtension(stableNameUtc);
        return $"{prefix}_{stamp}{ext}";
    }

    public async Task<UploadResponse> UploadVersionedAsync(Stream file, string? uploadedBy = null, CancellationToken ct = default)
    {
        return await _policy.ExecuteAsync(async ct2 =>
        {
            var versionedName = BuildVersionedName(_settings.StableBlobName);
            var versioned = _container.GetBlobClient(versionedName);

            file.Position = 0;
            var metadata = new Dictionary<string, string>
            {
                ["uploadedOnUtc"] = DateTimeOffset.UtcNow.ToString("o"),
                ["uploadedBy"] = uploadedBy ?? "unknown"
            };
            var tags = new Dictionary<string, string>
            {
                ["rules-kind"] = "rulesengine",
                ["is-latest"] = "false"
            };

            await versioned.UploadAsync(file, new BlobUploadOptions
            {
                Metadata = metadata,
                Tags = tags
            }, ct2);

            // copy to stable alias
            var stable = _container.GetBlobClient(_settings.StableBlobName);
            var start = await stable.StartCopyFromUriAsync(versioned.Uri, cancellationToken: ct2);

            // Optionally poll until finished (simple check)
            var props = await stable.GetPropertiesAsync(cancellationToken: ct2);
            if (props.Value.CopyStatus == CopyStatus.Pending)
            {
                // Wait/poll for copy completion - simple loop with timeout
                int attempts = 0;
                while (props.Value.CopyStatus == CopyStatus.Pending && attempts++ < 10)
                {
                    await Task.Delay(500, ct2);
                    props = await stable.GetPropertiesAsync(cancellationToken: ct2);
                }
            }

            // update metadata/tags
            await stable.SetMetadataAsync(metadata);
            await stable.SetTagsAsync(new Dictionary<string, string>
            {
                ["rules-kind"] = "rulesengine",
                ["is-latest"] = "true",
                ["source-version"] = versionedName
            });

            return new UploadResponse
            {
                StableBlobUrl = stable.Uri.ToString(),
                VersionedBlobUrl = versioned.Uri.ToString(),
                VersionedBlobName = versionedName,
                Message = "Rules uploaded; latest alias updated."
            };
        }, ct);
    }

    public async Task<IReadOnlyList<BlobVersionInfo>> ListVersionsAsync(CancellationToken ct = default)
    {
        return await _policy.ExecuteAsync(async ct2 =>
        {
            var results = new List<BlobVersionInfo>();
            await foreach (var item in _container.GetBlobsAsync(prefix: Path.GetFileNameWithoutExtension(_settings.StableBlobName), traits: BlobTraits.Metadata | BlobTraits.Tags, cancellationToken: ct2))
            {
                var client = _container.GetBlobClient(item.Name);
                results.Add(new BlobVersionInfo
                {
                    Name = item.Name,
                    LastModified = item.Properties.LastModified,
                    SizeBytes = item.Properties.ContentLength,
                    Metadata = item.Metadata ?? new Dictionary<string, string>(),
                    Tags = item.Tags?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, string>(),
                    Url = client.Uri.ToString()
                });
            }

            return results.OrderByDescending(x => x.LastModified ?? DateTimeOffset.MinValue).ToList();
        }, ct);
    }

    public async Task<string> GetLatestUrlAsync(CancellationToken ct = default)
    {
        return await _policy.ExecuteAsync(async ct2 =>
        {
            var stable = _container.GetBlobClient(_settings.StableBlobName);
            var exists = await stable.ExistsAsync(ct2);
            if (!exists) throw new FileNotFoundException("Latest alias not found.");
            return stable.Uri.ToString();
        }, ct);
    }

    public async Task<UploadResponse> RollbackAsync(string versionedName, string? requestedBy = null, CancellationToken ct = default)
    {
        return await _policy.ExecuteAsync(async ct2 =>
        {
            var source = _container.GetBlobClient(versionedName);
            if (!await source.ExistsAsync(ct2)) throw new FileNotFoundException("Version not found: " + versionedName);

            var stable = _container.GetBlobClient(_settings.StableBlobName);
            await stable.StartCopyFromUriAsync(source.Uri, cancellationToken: ct2);

            var metadata = new Dictionary<string, string>
            {
                ["rolledBackOnUtc"] = DateTimeOffset.UtcNow.ToString("o"),
                ["rolledBackBy"] = requestedBy ?? "unknown",
                ["source-version"] = versionedName
            };

            await stable.SetMetadataAsync(metadata);
            await stable.SetTagsAsync(new Dictionary<string, string>
            {
                ["rules-kind"] = "rulesengine",
                ["is-latest"] = "true",
                ["source-version"] = versionedName
            });

            return new UploadResponse
            {
                StableBlobUrl = stable.Uri.ToString(),
                VersionedBlobUrl = source.Uri.ToString(),
                VersionedBlobName = versionedName,
                Message = $"Rolled back latest to {versionedName}."
            };
        }, ct);
    }
}
