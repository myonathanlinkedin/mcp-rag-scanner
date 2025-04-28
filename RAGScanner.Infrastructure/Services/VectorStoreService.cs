using Microsoft.Extensions.Logging;

public class VectorStoreService : IVectorStoreService
{
    private readonly ILogger<VectorStoreService> _logger;

    public VectorStoreService(ILogger<VectorStoreService> logger)
    {
        _logger = logger;
    }

    public async Task SaveDocumentAsync(DocumentVector documentVector)
    {
        // TODO: Connect to Qdrant and insert the vector + metadata
        // Placeholder: Log what would be saved
        _logger.LogInformation("Saving vector for URL: {Url}", documentVector.Metadata.Url);

        await Task.CompletedTask;
    }
}
