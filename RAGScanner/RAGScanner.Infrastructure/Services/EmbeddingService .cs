using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
        var requestBody = new { model = applicationSettings.Api.EmbeddingModel, input };
        var jsonRequest = JsonConvert.SerializeObject(requestBody);
        var contentRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var endpoint = $"{applicationSettings.Api.Endpoint}/embeddings";

        var response = await httpClient.PostAsync(endpoint, contentRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Failed to generate embedding. Status Code: {response.StatusCode}. Response: {responseBody}");
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddingResponse = JsonConvert.DeserializeObject<EmbeddingResponse>(responseString)
            ?? throw new Exception("Failed to deserialize embedding response.");

        // Use null propagation and throw expression for more concise error handling.
        var embeddingData = embeddingResponse.Data?.FirstOrDefault()?.Embedding
            ?? throw new Exception("Embedding response contains no data.");

        return embeddingData;
    }
}
