using MediatR;
using Microsoft.Extensions.Logging;

public class ScanUrlCommand : IRequest<Result>
{
    public List<string> Urls { get; set; } = new();

    public class ScanUrlCommandHandler : IRequestHandler<ScanUrlCommand, Result>
    {
        private readonly IScraperService scraperService;
        private readonly IDocumentParserService documentParserService;
        private readonly IVectorStoreService vectorStoreService;
        private readonly IEmbeddingService embeddingService;
        private readonly ILogger logger;

        public ScanUrlCommandHandler(
            IScraperService scraperService,
            IDocumentParserService documentParserService,
            IVectorStoreService vectorStoreService,
            IEmbeddingService embeddingService,
            ILogger<ScanUrlCommandHandler> logger)
        {
            this.scraperService = scraperService;
            this.documentParserService = documentParserService;
            this.vectorStoreService = vectorStoreService;
            this.embeddingService = embeddingService;
            this.logger = logger;
        }

        public async Task<Result> Handle(ScanUrlCommand request, CancellationToken cancellationToken)
        {
            if (request.Urls == null || !request.Urls.Any())
            {
                return Result.Failure(new[] { "No URLs provided for scanning." });
            }

            var scrapedDocuments = await this.scraperService.ScrapeUrlsAsync(request.Urls);
            if (scrapedDocuments == null || !scrapedDocuments.Any())
            {
                return Result.Failure(new[] { "Failed to scrape the provided URLs." });
            }

            // Process each document using functional composition with Select and Task.WhenAll
            var processingTasks = scrapedDocuments.Select(doc => ProcessScrapedDocument(doc, cancellationToken));
            await Task.WhenAll(processingTasks);

            return Result.Success;
        }

        private async Task ProcessScrapedDocument(ScrapedDocument doc, CancellationToken cancellationToken)
        {
            IEnumerable<string> pageContents = doc.IsPdf
                ? this.documentParserService.ParsePdfPerPage(doc.ContentBytes)
                : new[] { this.documentParserService.ParseHtml(doc.ContentText) };

            // Use Select to transform page contents into tasks, and handle them with WhenAll
            var embeddingTasks = pageContents.Select((content, index) =>
            {
                int pageNumber = doc.IsPdf ? index + 1 : 0; // 0 for HTML
                return ProcessPageContent(doc.Url, content, pageNumber, cancellationToken);
            });

            await Task.WhenAll(embeddingTasks);
        }

        private async Task ProcessPageContent(string url, string content, int pageNumber, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                this.logger.LogWarning("Parsed content from URL '{Url}' was empty. Skipping.", url);
                return;
            }

            float[] embedding = await this.embeddingService.GenerateEmbeddingAsync(content, cancellationToken);
            var metadata = CreateMetadata(url, content, pageNumber);
            var documentVector = CreateDocumentVector(embedding, metadata);

            await this.vectorStoreService.SaveDocumentAsync(documentVector, embedding.Length);
        }

        private DocumentVector CreateDocumentVector(float[] embedding, DocumentMetadata metadata) =>
            new DocumentVector
            {
                Embedding = embedding,
                Metadata = metadata
            };

        private DocumentMetadata CreateMetadata(string url, string content, int pageNumber)
        {
            string title = pageNumber == 0 ? ExtractTitle(content) : $"Page {pageNumber}";
            string sourceType = pageNumber == 0 ? "html" : "pdf";
            return new DocumentMetadata
            {
                Url = url,
                SourceType = sourceType,
                Title = title,
                Content = content,
                ScrapedAt = DateTime.UtcNow
            };
        }

        private string ExtractTitle(string html)
        {
            const string titleTagStart = "<title>";
            const string titleTagEnd = "</title>";

            var start = html.IndexOf(titleTagStart, StringComparison.OrdinalIgnoreCase);
            var end = html.IndexOf(titleTagEnd, StringComparison.OrdinalIgnoreCase);

            return (start == -1 || end == -1 || end <= start)
                ? "Untitled"
                : html.Substring(start + titleTagStart.Length, end - start - titleTagStart.Length).Trim();
        }
    }
}