using MediatR;

public class ScanUrlCommand : IRequest<Result>
{
    public List<string> Urls { get; set; } = new();

    public class ScanUrlCommandHandler : IRequestHandler<ScanUrlCommand, Result>
    {
        private readonly IScraperService scraperService;
        private readonly IDocumentParserService documentParserService;
        private readonly IVectorStoreService vectorStoreService;
        private readonly IEmbeddingService embeddingService;

        public ScanUrlCommandHandler(
            IScraperService scraperService,
            IDocumentParserService documentParserService,
            IVectorStoreService vectorStoreService,
            IEmbeddingService embeddingService)
        {
            this.scraperService = scraperService;
            this.documentParserService = documentParserService;
            this.vectorStoreService = vectorStoreService;
            this.embeddingService = embeddingService;
        }

        public async Task<Result> Handle(ScanUrlCommand request, CancellationToken cancellationToken)
        {
            // Scrape all URLs
            var scrapedDocuments = await scraperService.ScrapeUrlsAsync(request.Urls);
            if (scrapedDocuments == null || scrapedDocuments.Count == 0)
            {
                return Result.Failure(new[] { "Failed to scrape the provided URLs." });
            }

            // Parse and create DocumentVectors
            foreach (var scrapedDocument in scrapedDocuments)
            {
                string parsedContent = scrapedDocument.IsPdf
                    ? documentParserService.ParsePdf(scrapedDocument.ContentBytes)
                    : documentParserService.ParseHtml(scrapedDocument.ContentText);

                // Generate embedding using the EmbeddingService
                float[] embedding = await embeddingService.GenerateEmbeddingAsync(parsedContent, cancellationToken);

                var documentVector = new DocumentVector
                {
                    Embedding = embedding,
                    Metadata = new DocumentMetadata
                    {
                        Url = scrapedDocument.Url,
                        SourceType = scrapedDocument.IsPdf ? "pdf" : "html",
                        Title = ExtractTitle(parsedContent),
                        ContentHash = HashHelper.ComputeDeterministicGuid(parsedContent),
                        ScrapedAt = scrapedDocument.ScrapedAt
                    }
                };

                // Save the document vector
                await vectorStoreService.SaveDocumentAsync(documentVector, embedding.Length);
            }

            return Result.Success;
        }

        private string ExtractTitle(string textContent)
        {
            var start = textContent.IndexOf("<title>", StringComparison.OrdinalIgnoreCase);
            var end = textContent.IndexOf("</title>", StringComparison.OrdinalIgnoreCase);

            if (start != -1 && end != -1 && end > start)
            {
                start += "<title>".Length;
                return textContent.Substring(start, end - start).Trim();
            }

            return "Untitled";
        }
    }
}
