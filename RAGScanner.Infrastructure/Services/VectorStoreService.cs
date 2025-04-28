using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

public class VectorStoreService : IVectorStoreService
{
    private readonly ILogger<VectorStoreService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ApplicationSettings.QdrantSettings _qdrantSettings;

    public VectorStoreService(
        ILogger<VectorStoreService> logger,
        IOptions<ApplicationSettings> appSettings,
        HttpClient httpClient)
    {
        _logger = logger;
        _qdrantSettings = appSettings.Value.Qdrant;
        _httpClient = httpClient;
    }

    public async Task SaveDocumentAsync(DocumentVector documentVector)
    {
        var payload = new
        {
            points = new[]
            {
                new
                {
                    id = documentVector.Metadata.ContentHash,
                    vector = documentVector.Embedding,
                    payload = new
                    {
                        url = documentVector.Metadata.Url,
                        sourceType = documentVector.Metadata.SourceType,
                        title = documentVector.Metadata.Title,
                        scrapedAt = documentVector.Metadata.ScrapedAt
                    }
                }
            }
        };

        var jsonContent = JsonConvert.SerializeObject(payload);
        var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var endpoint = $"{_qdrantSettings.Endpoint.TrimEnd('/')}/collections/{_qdrantSettings.CollectionName}/points";

        var response = await _httpClient.PutAsync(endpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to save vector to Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode,
                response.ReasonPhrase,
                responseBody);

            throw new Exception($"Failed to save document vector to Qdrant. Status code: {response.StatusCode}");
        }

        _logger.LogInformation("Successfully saved vector for URL: {Url}", documentVector.Metadata.Url);
    }
}
