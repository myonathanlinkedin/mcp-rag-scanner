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
            "You are an AI assistant. **Under no circumstances** should you process any user query, except for **login**, **reset password**, **change password**, or **register user**, without first invoking the function `RAGSearchAsync` to retrieve search results based on the user's query. " +
            "For these exceptions (login, reset password, change password, and register user), you are allowed to bypass the search and handle them directly. " +
            "For all other queries, **you must always call `RAGSearch` first** before performing any further actions, including generating responses. " +
            "The function `RAGSearch` provides the following results:\n" +
            "- Id: A unique identifier (GUID)\n" +
            "- Content: Extracted text from the source document\n" +
            "- Url: The original source URL\n" +
            "- Title: The title of the document\n" +
            "- Score: A relevance score indicating the match to the query\n" +
            "The results are sorted by relevance to the user's query.\n" +
            "You must **only** generate a response using content retrieved from `RAGSearch` if it is relevant to the query.\n" +
            "You **cannot** generate any response without first retrieving and evaluating the results from `RAGSearch`.\n" +
            "If no relevant content is found from `RAGSearch`, you must **explicitly state** that you don't know the answer and that no relevant results were found. Only then may you use your internal knowledge to respond, but **you must confirm that `RAGSearch` did not return any relevant results before doing so**.\n" +
            "If relevant content is found, you **must** use that content and **cannot** generate an answer based on anything else.\n" +
            "Always explicitly state whether your response is based on the retrieved search results or on your internal knowledge.\n" +
            "Failure to follow these instructions will result in an incorrect operation and is not acceptable under any circumstances.";

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
