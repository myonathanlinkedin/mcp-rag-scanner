using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

public class VectorStoreService : IVectorStoreService
{
    private readonly ILogger<VectorStoreService> logger;
    private readonly HttpClient httpClient;
    private readonly ApplicationSettings.QdrantSettings qdrantSettings;
    private readonly string baseEndpoint;
    private float similarityThreshold;

    public VectorStoreService(
        ILogger<VectorStoreService> logger,
        ApplicationSettings appSettings,
        HttpClient httpClient)
    {
        this.logger = logger;
        qdrantSettings = appSettings.Qdrant;
        this.httpClient = httpClient;
        baseEndpoint = qdrantSettings.Endpoint.TrimEnd('/');
        similarityThreshold = appSettings.Qdrant.SimilarityThreshold; // Load dynamic threshold
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
            logger.LogError(
                "Failed to create collection in Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode,
                response.ReasonPhrase,
                responseBody);

            throw new Exception($"Failed to create collection in Qdrant. Status code: {response.StatusCode}");
        }

        logger.LogInformation("Successfully created collection '{CollectionName}' in Qdrant.", qdrantSettings.CollectionName);
    }

    public async Task SaveDocumentAsync(DocumentVector documentVector, int vectorSize)
    {
        await EnsureCollectionExistsAsync(vectorSize);

        try
        {

            var existingVectors = await GetExistingVectorsAsync(documentVector.Embedding); // Use the vector to query

            // Use similarity check to determine if the vector should be stored
            foreach (var existingVector in existingVectors)
            {
                var similarity = VectorUtility.ComputeCosineSimilarity(documentVector.Embedding, existingVector.Embedding);

                if (similarity >= similarityThreshold)
                {
                    logger.LogInformation("Vector is too similar to an existing one (Similarity: {Similarity}). Skipping save.", similarity);
                    return;  // Skip saving the vector if it's too similar
                }
            }
        }
        catch (Exception ex)
        {
           // exception will rise if no document in the DB, continue insert
        }

        // Proceed to save the new vector as it's sufficiently distinct
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
            logger.LogError(
                "Failed to save vector to Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode,
                response.ReasonPhrase,
                responseBody);

            throw new Exception($"Failed to save document vector to Qdrant. Status code: {response.StatusCode}");
        }

        logger.LogInformation("Successfully saved vector for URL: {Url}", documentVector.Metadata.Url);
    }

    // Get existing vectors from Qdrant collection based on query vector
    private async Task<List<DocumentVector>> GetExistingVectorsAsync(float[] queryVector)
    {
        var endpoint = $"{baseEndpoint}/collections/{qdrantSettings.CollectionName}/points/query";
        var vectors = new List<DocumentVector>();

        var payload = new
        {
            query = queryVector
        };

        var jsonContent = JsonConvert.SerializeObject(payload);
        var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(endpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "Failed to retrieve vectors from Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode,
                response.ReasonPhrase,
                responseBody);

            throw new Exception($"Failed to retrieve document vectors from Qdrant. Status code: {response.StatusCode}");
        }

        // Parse the response body to extract the vectors
        var responseBodyContent = await response.Content.ReadAsStringAsync();
        var qdrantResponse = JsonConvert.DeserializeObject<QdrantQueryResponse>(responseBodyContent);

        if (qdrantResponse?.Result != null)
        {
            foreach (var result in qdrantResponse.Result)
            {
                var documentVector = new DocumentVector
                {
                    Metadata = new DocumentMetadata
                    {
                        Id = Guid.Parse(result.Id),
                        Url = result.Payload?.Url,
                        SourceType = result.Payload?.SourceType,
                        Title = result.Payload?.Title,
                        Content = result.Payload?.Content,
                        // Use TryParse for ScrapedAt
                        ScrapedAt = result.Payload?.ScrapedAt ?? default(DateTime) // Use null-coalescing operator to handle nullable DateTime
                    },
                    Embedding =  result.Vector.ToArray()
                };

                vectors.Add(documentVector);
            }
        }

        return vectors;
    }
}
