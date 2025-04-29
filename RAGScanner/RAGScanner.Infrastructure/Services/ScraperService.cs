using Microsoft.Extensions.Logging;

public class ScraperService : IScraperService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ScraperService> logger;

    public ScraperService(IHttpClientFactory httpClientFactory, ILogger<ScraperService> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<List<ScrapedDocument>> ScrapeUrlsAsync(List<string> urls)
    {
        var documents = new List<ScrapedDocument>();
        var client = httpClientFactory.CreateClient();

        foreach (var url in urls)
        {
            try
            {
                var response = await HttpHelper.GetAsync(client, url);
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var isPdf = ContentTypeDetector.IsPdf(contentType);

                var bytes = await response.Content.ReadAsByteArrayAsync();
                string contentText = null;

                if (!isPdf)
                {
                    // If it's not PDF, read as string
                    contentText = await response.Content.ReadAsStringAsync();
                }

                documents.Add(new ScrapedDocument
                {
                    Url = url,
                    ContentBytes = bytes,
                    ContentText = contentText,
                    IsPdf = isPdf,
                    ScrapedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error scraping {Url}", url);
            }
        }

        return documents;
    }
}
