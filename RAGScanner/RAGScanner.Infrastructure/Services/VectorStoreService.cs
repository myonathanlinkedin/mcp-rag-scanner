using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

public class VectorStoreService : IVectorStoreService
{
    private readonly ILogger<VectorStoreService> logger;
    private readonly HttpClient httpClient;
    private readonly ApplicationSettings.QdrantSettings qdrantSettings;
    private readonly string baseEndpoint;
    private readonly float similarityThreshold;

    public VectorStoreService(
        ILogger<VectorStoreService> logger,
        ApplicationSettings appSettings,
        HttpClient httpClient)
    {
        this.logger = logger;
        qdrantSettings = appSettings.Qdrant;
        this.httpClient = httpClient;
        baseEndpoint = qdrantSettings.Endpoint.TrimEnd('/');
        similarityThreshold = appSettings.Qdrant.SimilarityThreshold;
    }

    private async Task EnsureCollectionExistsAsync(int vectorSize)
    {
        var endpoint = $"{baseEndpoint}/collections/{qdrantSettings.CollectionName}";
        var response = await httpClient.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Collection '{CollectionName}' not found. Attempting to create it.", qdrantSettings.CollectionName);
            await CreateCollectionAsync(vectorSize);
        }
        else
        {
            logger.LogInformation("Collection '{CollectionName}' exists.", qdrantSettings.CollectionName);
        }
    }

    private async Task CreateCollectionAsync(int vectorSize)
    {
        var endpoint = $"{baseEndpoint}/collections/{qdrantSettings.CollectionName}";
        var payload = new
        {
            vectors = new
            {
                size = vectorSize,
                distance = "Cosine"
            }
        };
        var jsonContent = JsonConvert.SerializeObject(payload);
        var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = await httpClient.PutAsync(endpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var errorMessage = $"Failed to create collection in Qdrant. Status code: {response.StatusCode}";
            logger.LogError(
                "Failed to create collection in Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode, response.ReasonPhrase, responseBody);
            throw new Exception(errorMessage);
        }

        logger.LogInformation("Successfully created collection '{CollectionName}' in Qdrant.", qdrantSettings.CollectionName);
    }

    public async Task SaveDocumentAsync(DocumentVector documentVector, int vectorSize)
    {
        await EnsureCollectionExistsAsync(vectorSize);

        try
        {
            var existingVectors = await GetExistingVectorsAsync(documentVector.Embedding);

            // Functional approach to check similarity
            if (existingVectors.Any(existingVector =>
                VectorUtility.ComputeCosineSimilarity(documentVector.Embedding, existingVector.Embedding) >= similarityThreshold))
            {
                logger.LogInformation("Vector is too similar to an existing one. Skipping save.");
                return;
            }
        }
        catch (Exception ex)
        {
            // Log the exception and continue
            logger.LogError(ex, "Error checking for similar vectors. Proceeding with save.");
        }

        var payload = new
        {
            points = new[]
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    vector = documentVector.Embedding,
                    payload = new
                    {
                        url = documentVector.Metadata.Url,
                        sourceType = documentVector.Metadata.SourceType,
                        title = documentVector.Metadata.Title,
                        content = documentVector.Metadata.Content,
                        scrapedAt = documentVector.Metadata.ScrapedAt
                    }
                }
            }
        };
        var jsonContent = JsonConvert.SerializeObject(payload);
        var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var endpoint = $"{baseEndpoint}/collections/{qdrantSettings.CollectionName}/points";
        var response = await httpClient.PutAsync(endpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var errorMessage = $"Failed to save vector to Qdrant. Status code: {response.StatusCode}";
            logger.LogError(
                "Failed to save vector to Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode, response.ReasonPhrase, responseBody);
            throw new Exception(errorMessage);
        }

        logger.LogInformation("Successfully saved vector for URL: {Url}", documentVector.Metadata.Url);
    }

    private async Task<List<DocumentVector>> GetExistingVectorsAsync(float[] queryVector)
    {
        var endpoint = $"{baseEndpoint}/collections/{qdrantSettings.CollectionName}/points/query";
        var payload = new { query = queryVector };
        var jsonContent = JsonConvert.SerializeObject(payload);
        var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(endpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var errorMessage = "Failed to retrieve vectors from Qdrant.";
            logger.LogError(
               "Failed to retrieve vectors from Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
               response.StatusCode, response.ReasonPhrase, responseBody);
            throw new Exception(errorMessage);
        }

        var responseBodyContent = await response.Content.ReadAsStringAsync();
        var qdrantResponse = JsonConvert.DeserializeObject<QdrantQueryResponse>(responseBodyContent);

        // Use LINQ for projection and null handling
        return qdrantResponse?.Result?.Select(result => new DocumentVector
        {
            Metadata = new DocumentMetadata
            {
                Id = Guid.Parse(result.Id),
                Url = result.Payload?.Url,
                SourceType = result.Payload?.SourceType,
                Title = result.Payload?.Title,
                Content = result.Payload?.Content,
                ScrapedAt = result.Payload?.ScrapedAt ?? default(DateTime)
            },
            Embedding = result.Vector.ToArray()
        }).ToList() ?? new List<DocumentVector>(); // Handle null qdrantResponse
    }
}
