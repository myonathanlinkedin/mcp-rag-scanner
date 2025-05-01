using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;
using System.Net.Http.Headers;

namespace MCP.Server.Tools
{
    [McpServerToolType]
    public sealed class RAGTools
    {
        private const string JsonMediaType = "application/json";
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string baseUrl;

        private const string ScanUrlsDescription =
            "Scan one or more URLs, parse the content, and save the resulting document vectors into the vector store. " +
            "You must be logged in to use this feature.";

        private const string RAGSearchDescription =
            "Perform Retrieval-Augmented Generation (RAG) to answer user queries. " +
            "You will receive a list of documents retrieved based on their relevance to a query. " +
            "Each document includes: " +
            "- Id: A unique identifier (GUID) for the document. " +
            "- Content: The extracted text content from the document. " +
            "- Url: The original URL of the document. " +
            "- Title: The title of the document. " +
            "- Score: A relevance score indicating the match to the query. " +
            "The results are sorted by relevance to the query. " +
            "If no relevant information is found, the result will indicate that no relevant content is available. " +
            "You are expected to base the response solely on the retrieved documents, and not to invent information.";

        public RAGTools(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this.httpClientFactory = httpClientFactory;
            baseUrl = configuration.GetSection("MCP:BaseUrl").Value;
        }

        [McpServerTool, Description(ScanUrlsDescription)]
        public async Task<string> ScanUrlsAsync(
            [Description("List of URLs to scan and process")] List<string> urls,
            [Description("The Bearer token obtained after login for authentication")] string token)
        {
            try
            {
                var payload = new
                {
                    Urls = urls
                };

                var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsJsonAsync("/api/RAGScanner/ScanUrl/ScanUrl", payload);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("Successfully scanned URLs: {Urls}", string.Join(", ", urls));
                    return "Successfully scanned and processed the URLs.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to scan URLs: {Urls}, StatusCode: {StatusCode}, Error: {Error}",
                        string.Join(", ", urls), response.StatusCode, errorContent);
                    return $"Failed to scan URLs. Status code: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while scanning URLs: {Urls}", string.Join(", ", urls));
                return "An error occurred while scanning URLs.";
            }
        }

        // New RAGSearch functionality
        [McpServerTool, Description(RAGSearchDescription)]
        public async Task<List<RAGSearchResult>> RAGSearchAsync(
          [Description("The search query")] string query,
          [Description("The Bearer token obtained after login for authentication")] string token)
        {
            try
            {
                var payload = new
                {
                    Query = query
                };

                var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsJsonAsync("/api/RAGScanner/RAGSearch/RAGSearch", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<RAGSearchResult>>();
                    Log.Information("Successfully performed RAG search with query: {Query}", query);
                    return result ?? new List<RAGSearchResult>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to perform RAG search for query: {Query}, StatusCode: {StatusCode}, Error: {Error}",
                        query, response.StatusCode, errorContent);
                    return new List<RAGSearchResult>();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while performing RAG search for query: {Query}", query);
                return new List<RAGSearchResult>();
            }
        }
    }
}
