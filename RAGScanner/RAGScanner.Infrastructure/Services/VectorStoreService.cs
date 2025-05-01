using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
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

    // Ensure collection exists
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

    // Create collection if doesn't exist
    private async Task CreateCollectionAsync(int vectorSize)
    {
        var endpoint = $"{baseEndpoint}/collections/{qdrantSettings.CollectionName}";

        try
        {
            var collectionExists = await CollectionExistsAsync();
            if (collectionExists)
            {
                logger.LogInformation("Collection '{CollectionName}' already exists in Qdrant. Skipping creation.", qdrantSettings.CollectionName);
                return; // Skip creation if collection exists
            }

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

                // Log and continue if conflict occurs (409 Conflict)
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.LogInformation("Collection '{CollectionName}' already exists. Skipping creation.", qdrantSettings.CollectionName);
                    return; // Skip creation if conflict (409)
                }

                // For other errors, throw an exception
                throw new Exception(errorMessage);
            }

            logger.LogInformation("Successfully created collection '{CollectionName}' in Qdrant.", qdrantSettings.CollectionName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during collection creation in Qdrant.");
            // Continue processing, do not throw an exception to allow insertion to proceed
        }
    }

    // Check if collection exists
    private async Task<bool> CollectionExistsAsync()
    {
        var checkEndpoint = $"{baseEndpoint}/collections/{qdrantSettings.CollectionName}";
        var response = await httpClient.GetAsync(checkEndpoint);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false; // Collection doesn't exist
        }

        if (response.IsSuccessStatusCode)
        {
            return true; // Collection exists
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        logger.LogError(
            "Error checking collection existence. StatusCode: {StatusCode}, Response: {ResponseBody}",
            response.StatusCode, responseBody);

        return false; // Proceed with creation in case of any other errors
    }

    // Save document vector after checking for similar vectors
    public async Task SaveDocumentAsync(DocumentVector documentVector, int vectorSize)
    {
        await EnsureCollectionExistsAsync(vectorSize);

        try
        {
            // Step 1: Search for similar vectors
            if (await IsVectorSimilarAsync(documentVector.Embedding))
            {
                logger.LogInformation("Vector is too similar to an existing one. Skipping save.");
                return; // Skip if vector is too similar
            }

            // Step 2: Insert the vector into Qdrant
            await InsertVectorAsync(documentVector);
            logger.LogInformation("Successfully saved vector for URL: {Url}", documentVector.Metadata.Url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving document vector.");
            throw;
        }
    }

    // Fix for CS1525 and CS0746 errors
    private async Task<bool> IsVectorSimilarAsync(float[] queryVector)
    {
        var endpoint = $"{baseEndpoint}/collections/{qdrantSettings.CollectionName}/points/search";
        var payload = new
        {
            vector = queryVector,
            limit = 5,  // Get the top 5 similar vectors
            @params = new { method = "cosine" } // Use @ to escape the reserved keyword 'params'
        };

        var jsonContent = JsonConvert.SerializeObject(payload);
        var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(endpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "Error during vector search in Qdrant. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseBody}",
                response.StatusCode, response.ReasonPhrase, responseBody);
            throw new Exception("Error during vector search.");
        }

        var responseBodyContent = await response.Content.ReadAsStringAsync();
        var qdrantResponse = JsonConvert.DeserializeObject<QdrantQueryResponse>(responseBodyContent);

        // Compare cosine similarity to the top result
        if (qdrantResponse?.Result?.Any() == true)
        {
            var mostSimilarVector = qdrantResponse.Result.First();
            var similarityScore = mostSimilarVector.Score;

            // Return true if the similarity score is greater than or equal to the threshold
            return similarityScore >= similarityThreshold;
        }

        return false; // No similar vectors found, so it's safe to insert
    }

    // Insert vector into Qdrant
    private async Task InsertVectorAsync(DocumentVector documentVector)
    {
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
}
