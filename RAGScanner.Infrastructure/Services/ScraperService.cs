using Microsoft.Extensions.Logging;

public class ScraperService : IScraperService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ScraperService> _logger;

    public ScraperService(IHttpClientFactory httpClientFactory, ILogger<ScraperService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<ScrapedDocument>> ScrapeUrlsAsync(List<string> urls)
    {
        var documents = new List<ScrapedDocument>();
        var client = _httpClientFactory.CreateClient();

        foreach (var url in urls)
        {
            try
            {
                var response = await HttpHelper.GetAsync(client, url);
                var contentType = response.Content.Headers.ContentType.MediaType;
                var isPdf = ContentTypeDetector.IsPdf(contentType);

                var bytes = await response.Content.ReadAsByteArrayAsync();
                documents.Add(new ScrapedDocument
                {
                    Url = url,
                    ContentBytes = bytes,
                    IsPdf = isPdf,
                    ScrapedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping {Url}", url);
            }
        }

        return documents;
    }
}