using Microsoft.Extensions.Logging;

public class UrlScanJobService : IUrlScanJobService
{
    private readonly IScraperService scraperService;
    private readonly IDocumentParserService parserService;
    private readonly IVectorStoreService vectorStore;
    private readonly IEmbeddingService embeddingService;
    private readonly IJobStatusStore jobStatusStore;
    private readonly ILogger<UrlScanJobService> logger;

    public UrlScanJobService(
        IScraperService scraperService,
        IDocumentParserService parserService,
        IVectorStoreService vectorStore,
        IEmbeddingService embeddingService,
        IJobStatusStore jobStatusStore,
        ILogger<UrlScanJobService> logger)
    {
        this.scraperService = scraperService;
        this.parserService = parserService;
        this.vectorStore = vectorStore;
        this.embeddingService = embeddingService;
        this.jobStatusStore = jobStatusStore;
        this.logger = logger;
    }

    public async Task ProcessAsync(List<string> urls, Guid jobId)
    {
        await UpdateJobStatus(jobId, JobStatusType.InProgress, "Processing");

        var scrapedDocs = await TryScrape(urls);
        if (!scrapedDocs.Any())
        {
            await UpdateJobStatus(jobId, JobStatusType.Failed, "Nothing scraped.");
            return;
        }

        var tasks = scrapedDocs.SelectMany(doc =>
        {
            IEnumerable<string> contents = doc.IsPdf
                ? parserService.ParsePdfPerPage(doc.ContentBytes)
                : new[] { parserService.ParseHtml(doc.ContentText) };

            return contents
                .Select((content, index) => new { content, index })
                .Where(x => !string.IsNullOrWhiteSpace(x.content))
                .Select(x => ProcessPage(doc, x.content, x.index));
        });

        await Task.WhenAll(tasks);

        await UpdateJobStatus(jobId, JobStatusType.Completed, "Completed");
    }

    private async Task<IEnumerable<ScrapedDocument>> TryScrape(List<string> urls)
    {
        try
        {
            return await scraperService.ScrapeUrlsAsync(urls);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scraping failed.");
            return Enumerable.Empty<ScrapedDocument>();
        }
    }

    private async Task ProcessPage(ScrapedDocument doc, string content, int pageIndex)
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(content, default);
        var metadata = new DocumentMetadata
        {
            Url = doc.Url,
            Title = pageIndex == 0 ? ExtractTitle(content) : $"Page {pageIndex + 1}",
            SourceType = doc.IsPdf ? "pdf" : "html",
            Content = content,
            ScrapedAt = DateTime.UtcNow
        };

        await vectorStore.SaveDocumentAsync(new DocumentVector { Embedding = embedding, Metadata = metadata }, embedding.Length);
    }

    private async Task UpdateJobStatus(Guid jobId, JobStatusType status, string message)
    {
        try
        {
            jobStatusStore.UpdateJobStatus(jobId.ToString(), status, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update job status.");
        }
    }

    private string ExtractTitle(string html)
    {
        const string startTag = "<title>", endTag = "</title>";
        var start = html.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        var end = html.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);
        return (start == -1 || end == -1 || end <= start)
            ? "Untitled"
            : html.Substring(start + startTag.Length, end - start - startTag.Length).Trim();
    }
}
