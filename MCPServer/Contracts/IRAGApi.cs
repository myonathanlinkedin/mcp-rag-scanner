using Refit;

public interface IRAGApi
{
    [Post("/api/RAGScanner/ScanUrl/ScanUrl")]
    Task<HttpResponseMessage> ScanUrlsAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/RAGScanner/RAGSearch/RAGSearch")]
    Task<List<RAGSearchResult>> RAGSearchAsync([Body] object payload, [Header("Authorization ")] string token);
}