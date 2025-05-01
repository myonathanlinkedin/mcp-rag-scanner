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
        var client = httpClientFactory.CreateClient();

        // Use LINQ to process URLs and handle potential errors
        var tasks = urls.Select(async url =>
        {
            try
            {
                var response = await HttpHelper.GetAsync(client, url);
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var isPdf = ContentTypeDetector.IsPdf(contentType);
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentText = !isPdf ? await response.Content.ReadAsStringAsync() : null;

                return new ScrapedDocument
                {
                    Url = url,
                    ContentBytes = bytes,
                    ContentText = contentText,
                    IsPdf = isPdf,
                    ScrapedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error scraping {Url}", url);
                // Return null for failed scrapes, filter them out later
                return null;
            }
        });

        // Await all tasks and filter out null results (failed scrapes)
        var results = await Task.WhenAll(tasks);
        return results.Where(doc => doc != null).ToList();
    }
}
