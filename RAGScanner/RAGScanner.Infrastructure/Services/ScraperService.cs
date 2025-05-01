using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

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

        var tasks = urls.Select(async url =>
        {
            try
            {
                var response = await HttpHelper.GetAsync(client, url);
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var isPdf = ContentTypeDetector.IsPdf(contentType);

                byte[] contentBytes;
                string contentText = null;

                if (isPdf)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    using var ms = new MemoryStream();
                    var buffer = new byte[81920]; // 80 KB buffer for better performance

                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                    {
                        await ms.WriteAsync(buffer.AsMemory(0, bytesRead));
                    }

                    contentBytes = ms.ToArray();
                }
                else
                {
                    contentBytes = await response.Content.ReadAsByteArrayAsync();
                    contentText = await response.Content.ReadAsStringAsync();
                }

                return new ScrapedDocument
                {
                    Url = url,
                    ContentBytes = contentBytes,
                    ContentText = contentText,
                    IsPdf = isPdf,
                    ScrapedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to scrape URL: {Url}", url);
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).ToList();
    }
}
