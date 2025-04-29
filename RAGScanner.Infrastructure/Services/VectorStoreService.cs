using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

public class VectorStoreService : IVectorStoreService
{
    private readonly ILogger<VectorStoreService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ApplicationSettings.QdrantSettings _qdrantSettings;
    private readonly string _baseEndpoint;
    private float _similarityThreshold;

    public VectorStoreService(
        ILogger<VectorStoreService> logger,
        IOptions<ApplicationSettings> appSettings,
        HttpClient httpClient)
    {
        _logger = logger;
        _qdrantSettings = appSettings.Value.Qdrant;
        _httpClient = httpClient;
        _baseEndpoint = _qdrantSettings.Endpoint.TrimEnd('/');
        _similarityThreshold = appSettings.Value.Qdrant.SimilarityThreshold; // Load dynamic threshold
    }

    private async Task EnsureCollectionExistsAsync(int vectorSize)
    {
        var endpoint = $"{_baseEndpoint}/collections/{_qdrantSettings.CollectionName}";
        var response = await _httpClient.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Collection '{CollectionName}' not found. Attempting to create it.", _qdrantSettings.CollectionName);
            await CreateCollectionAsync(vectorSize);
        }
        else
        {
            _logger.LogInformation("Collection '{CollectionName}' exists.", _qdrantSettings.CollectionName);
        }
    }

    private async Task CreateCollectionAsync(int vectorSize)
    {
        var endpoint = $"{_baseEndpoint}/collections/{_qdrantSettings.CollectionName}";

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

        var response = await _httpClient.PutAsync(endpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to create collection in Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode,
                response.ReasonPhrase,
                responseBody);

            throw new Exception($"Failed to create collection in Qdrant. Status code: {response.StatusCode}");
        }

        _logger.LogInformation("Successfully created collection '{CollectionName}' in Qdrant.", _qdrantSettings.CollectionName);
    }

    public async Task SaveDocumentAsync(DocumentVector documentVector, int vectorSize)
    {
        await EnsureCollectionExistsAsync(vectorSize);

        var existingVectors = await GetExistingVectorsAsync(documentVector.Embedding); // Use the vector to query

        // Use similarity check to determine if the vector should be stored
        foreach (var existingVector in existingVectors)
        {
            var similarity = ComputeCosineSimilarity(documentVector.Embedding, existingVector.Embedding);

            if (similarity >= _similarityThreshold)
            {
                _logger.LogInformation("Vector is too similar to an existing one (Similarity: {Similarity}). Skipping save.", similarity);
                return;  // Skip saving the vector if it's too similar
            }
        }

        // Proceed to save the new vector as it's sufficiently distinct
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

        var endpoint = $"{_baseEndpoint}/collections/{_qdrantSettings.CollectionName}/points";
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

    // Helper method to compute cosine similarity between two vectors
    private float ComputeCosineSimilarity(float[] vector1, float[] vector2)
    {
        var dotProduct = 0f;
        var magnitude1 = 0f;
        var magnitude2 = 0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0f;  // Prevent division by zero

        return dotProduct / (magnitude1 * magnitude2);
    }

    // Get existing vectors from Qdrant collection based on query vector
    private async Task<List<DocumentVector>> GetExistingVectorsAsync(float[] queryVector)
    {
        var endpoint = $"{_baseEndpoint}/collections/{_qdrantSettings.CollectionName}/points/query";
        var vectors = new List<DocumentVector>();

        var payload = new
        {
            query = queryVector
        };

        var jsonContent = JsonConvert.SerializeObject(payload);
        var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Failed to retrieve vectors from Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode,
                response.ReasonPhrase,
                responseBody);

            throw new Exception($"Failed to retrieve document vectors from Qdrant. Status code: {response.StatusCode}");
        }

        // Parse the response body to extract the vectors
        var responseBodyContent = await response.Content.ReadAsStringAsync();
        var qdrantResponse = JsonConvert.DeserializeObject<QdrantQueryResponse>(responseBodyContent);

        if (qdrantResponse?.Results != null)
        {
            foreach (var result in qdrantResponse.Results)
            {
                var documentVector = new DocumentVector
                {
                    Metadata = new DocumentMetadata
                    {
                        ContentHash = result.Id,
                        Url = result.Payload?.Url,
                        SourceType = result.Payload?.SourceType,
                        Title = result.Payload?.Title,
                        // Use TryParse for ScrapedAt
                        ScrapedAt = DateTime.TryParse(result.Payload?.ScrapedAt, out DateTime parsedDate) ? parsedDate : default
                    },
                    Embedding = result.Vector
                };

                vectors.Add(documentVector);
            }
        }

        return vectors;
    }
}
