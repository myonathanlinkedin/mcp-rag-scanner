using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static RAGSearchCommand;

public class RAGSearchCommand : IRequest<Result<List<RAGSearchResult>>>
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5; // number of top documents to retrieve

    public class RAGSearchCommandHandler : IRequestHandler<RAGSearchCommand, Result<List<RAGSearchResult>>>
    {
        private readonly IRetrieverService retrieverService;
        private readonly IEmbeddingService embeddingService;

        public RAGSearchCommandHandler(IRetrieverService retrieverService, IEmbeddingService embeddingService)
        {
            this.retrieverService = retrieverService;
            this.embeddingService = embeddingService;
        }

        public async Task<Result<List<RAGSearchResult>>> Handle(RAGSearchCommand request, CancellationToken cancellationToken)
        {
            // 1. Retrieve relevant documents based on the query
            var searchResults = await retrieverService.RetrieveDocumentsByQueryAsync(request.Query, cancellationToken);

            if (!searchResults.Succeeded)
            {
                return Result<List<RAGSearchResult>>.Failure(new List<string> { "Failed to retrieve documents for query." });
            }

            // 2. Get the embedding for the query
            var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);

            // 3. Compute the similarity score for each document based on the query embedding
            var ragSearchResults = searchResults.Data.Select(doc =>
            {
                var documentEmbedding = doc.Embedding; // Assuming document embeddings are available in the doc
                var score = VectorUtility.ComputeCosineSimilarity(queryEmbedding, documentEmbedding); // Cosine similarity between query and document embedding

                return new RAGSearchResult
                {
                    Id = doc.Metadata.Id, // Assuming each document has a unique ID
                    Content = doc.Metadata.Content, // Assuming content is part of the title (adjust as needed)
                    Url = doc.Metadata.Url,
                    Title = doc.Metadata.Title,
                    Score = score,
                };
            }).ToList();

            // 4. Sort the results by score in descending order and limit to TopK
            var topResults = ragSearchResults.OrderByDescending(result => result.Score).Take(request.TopK).ToList();

            return Result<List<RAGSearchResult>>.SuccessWith(topResults);
        }
    }
}
