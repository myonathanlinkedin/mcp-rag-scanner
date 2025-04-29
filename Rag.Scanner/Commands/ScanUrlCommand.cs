using MediatR;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

public class ScanUrlCommand : IRequest<Result>
{
    public List<string> Urls { get; set; } = new();

    public class ScanUrlCommandHandler : IRequestHandler<ScanUrlCommand, Result>
    {
        private readonly IScraperService scraperService;
        private readonly IDocumentParserService documentParserService;
        private readonly IVectorStoreService vectorStoreService;
        private readonly ApplicationSettings applicationSettings;
        private readonly HttpClient httpClient;

        public ScanUrlCommandHandler(
            IScraperService scraperService,
            IDocumentParserService documentParserService,
            IVectorStoreService vectorStoreService,
            ApplicationSettings applicationSettings,
            HttpClient httpClient)
        {
            this.scraperService = scraperService;
            this.documentParserService = documentParserService;
            this.vectorStoreService = vectorStoreService;
            this.applicationSettings = applicationSettings;
            this.httpClient = httpClient;
        }

        public async Task<Result> Handle(ScanUrlCommand request, CancellationToken cancellationToken)
        {
            // 1. Scrape all URLs
            var scrapedDocuments = await scraperService.ScrapeUrlsAsync(request.Urls);

            if (scrapedDocuments == null || scrapedDocuments.Count == 0)
            {
                return Result.Failure(new[] { "Failed to scrape the provided URLs." });
            }

            // 2. Parse and create DocumentVectors
            foreach (var scrapedDocument in scrapedDocuments)
            {
                string parsedContent;

                if (scrapedDocument.IsPdf)
                {
                    parsedContent = documentParserService.ParsePdf(scrapedDocument.ContentBytes);
                }
                else
                {
                    parsedContent = documentParserService.ParseHtml(scrapedDocument.ContentText);
                }

                // Generate embedding for parsedContent
                float[] embedding = await GenerateEmbeddingAsync(parsedContent, cancellationToken);

                var metadata = new DocumentMetadata
                {
                    Url = scrapedDocument.Url,
                    SourceType = scrapedDocument.IsPdf ? "pdf" : "html",
                    Title = ExtractTitle(parsedContent),
                    ContentHash = HashHelper.ComputeDeterministicGuid(parsedContent),
                    ScrapedAt = scrapedDocument.ScrapedAt
                };

                var documentVector = new DocumentVector
                {
                    Embedding = embedding,
                    Metadata = metadata
                };

                // Calculate the vector size
                int vectorSize = embedding.Length;

                // Save the document vector with the vector size
                await vectorStoreService.SaveDocumentAsync(documentVector, vectorSize);
            }

            return Result.Success;
        }

        private async Task<float[]> GenerateEmbeddingAsync(string content, CancellationToken cancellationToken)
        {
            var requestBody = new
            {
                model = applicationSettings.Api.EmbeddingModel,
                input = content
            };

            var jsonRequest = JsonConvert.SerializeObject(requestBody);
            var contentRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{applicationSettings.Api.Endpoint}/embeddings", contentRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Failed to generate embedding. Status Code: {response.StatusCode}. Response: {responseBody}");
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var embeddingResponse = JsonConvert.DeserializeObject<EmbeddingResponse>(responseString)
                ?? throw new Exception("Failed to deserialize embedding response.");

            if (embeddingResponse.Data == null || embeddingResponse.Data.Count == 0)
            {
                throw new Exception("Embedding response contains no data.");
            }

            return embeddingResponse.Data[0].Embedding;
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