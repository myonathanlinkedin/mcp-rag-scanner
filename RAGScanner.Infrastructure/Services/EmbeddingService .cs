using Newtonsoft.Json;
using System.Text;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient httpClient;
    private readonly ApplicationSettings applicationSettings;

    public EmbeddingService(HttpClient httpClient, ApplicationSettings applicationSettings)
    {
        this.httpClient = httpClient;
        this.applicationSettings = applicationSettings;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = applicationSettings.Api.EmbeddingModel,
            input = input
        };

        var jsonRequest = JsonConvert.SerializeObject(requestBody);
        var contentRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{applicationSettings.Api.Endpoint}/embeddings", contentRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Failed to generate embedding. Status Code: {response.StatusCode}. Response: {responseBody}");
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddingResponse = JsonConvert.DeserializeObject<EmbeddingResponse>(responseString)
            ?? throw new Exception("Failed to deserialize embedding response.");

        if (embeddingResponse.Data == null || embeddingResponse.Data.Count == 0)
        {
            throw new Exception("Embedding response contains no data.");
        }

        return embeddingResponse.Data[0].Embedding;
    }
}
