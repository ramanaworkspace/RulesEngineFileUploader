namespace Infrastructure;

public sealed class AzureStorageSettings
{
    public string AccountUrl { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "rulesengine";
    public string StableBlobName { get; set; } = "RulesEngine.json";
}
