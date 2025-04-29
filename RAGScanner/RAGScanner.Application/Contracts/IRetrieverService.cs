public interface IRetrieverService
{
    Task<Result<List<DocumentVector>>> RetrieveAllDocumentsAsync(CancellationToken cancellationToken);
    Task<Result<List<DocumentVector>>> RetrieveDocumentsByQueryAsync(string queryText, CancellationToken cancellationToken);
}
