using MediatR;
using Microsoft.Extensions.Logging;

public class RAGSearchCommand : IRequest<Result<List<RAGSearchResult>>>
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 1; // number of top documents to retrieve

    public class RAGSearchCommandHandler : IRequestHandler<RAGSearchCommand, Result<List<RAGSearchResult>>>
    {
        private readonly IRetrieverService retrieverService;
        private readonly IEmbeddingService embeddingService;
        private readonly ILogger<RAGSearchCommandHandler> logger;

        public RAGSearchCommandHandler(IRetrieverService retrieverService, IEmbeddingService embeddingService, ILogger<RAGSearchCommandHandler> logger)
        {
            this.retrieverService = retrieverService;
            this.embeddingService = embeddingService;
            this.logger = logger;
        }

        public async Task<Result<List<RAGSearchResult>>> Handle(RAGSearchCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling RAGSearchCommand for query: {Query}, TopK: {TopK}", request.Query, request.TopK);

            try
            {
                // 1. Retrieve relevant documents based on the query
                var searchResults = await retrieverService.RetrieveDocumentsByQueryAsync(request.Query, cancellationToken);

                if (!searchResults.Succeeded)
                {
                    logger.LogError("Failed to retrieve documents: {Errors}", string.Join(", ", searchResults.Errors));
                    return Result<List<RAGSearchResult>>.Failure(searchResults.Errors);
                }

                logger.LogInformation("Successfully retrieved {Count} documents from retriever", searchResults.Data.Count);

                // 2. Get the embedding for the query
                var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
                logger.LogInformation("Generated embedding for the query");

                // 3. Compute similarity, map to RAGSearchResult, sort, and take top K
                var topResults = searchResults.Data
                    .Select(doc =>
                    {
                        try
                        {
                            // Compute cosine similarity between the query's embedding and document's embedding
                            var similarityScore = VectorUtility.ComputeCosineSimilarity(queryEmbedding, doc.Embedding);

                            // Map to RAGSearchResult
                            return new RAGSearchResult
                            {
                                Id = doc.Metadata.Id,
                                Content = doc.Metadata.Content,
                                Url = doc.Metadata.Url,
                                Title = doc.Metadata.Title,
                                Score = similarityScore,  // Store the similarity score here
                            };
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error mapping document to RAGSearchResult");
                            return null; // Handle error, e.g., return null if there's an issue with a document
                        }
                    })
                    .Where(result => result != null) // Filter out any null results due to mapping errors
                    .OrderByDescending(result => result.Score) // Sort by similarity score (cosine similarity)
                    .Take(request.TopK) // Take top K results
                    .ToList(); // Execute and materialize the result

                logger.LogInformation("Returning top {TopK} results", request.TopK);
                return Result<List<RAGSearchResult>>.SuccessWith(topResults);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling RAGSearchCommand");
                return Result<List<RAGSearchResult>>.Failure(new List<string> { "An error occurred during the search." }); // Return a failure result
            }
        }
    }
}
