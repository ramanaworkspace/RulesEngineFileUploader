using Application.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Wrap;

namespace Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly AzureStorageSettings _settings;
    private readonly BlobContainerClient _container;
    private readonly AsyncPolicyWrap _policy;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly BlobContainerClient _containerClient;
    public BlobStorageService(IOptions<AzureStorageSettings> options)
    {
        var settings = options.Value;
        var blobServiceClient = new BlobServiceClient(settings.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(settings.ContainerName);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(fileStream, overwrite: true);
        return blobClient.Uri.ToString();
    }
}