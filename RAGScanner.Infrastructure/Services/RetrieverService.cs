using Microsoft.Extensions.Logging;

public class RetrieverService : IRetrieverService
{
    private readonly ILogger<RetrieverService> _logger;

    public RetrieverService(ILogger<RetrieverService> logger)
    {
        _logger = logger;
    }

    public async Task<List<DocumentVector>> RetrieveAllDocumentsAsync()
    {
        // TODO: Connect to Qdrant and fetch all vectors + metadata
        _logger.LogInformation("Retrieving all documents from vector store");

        return await Task.FromResult(new List<DocumentVector>());
    }
}
