namespace Infrastructure;

public sealed class AzureStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string StableBlobName { get; set; } = "RulesEngine.json";
}
