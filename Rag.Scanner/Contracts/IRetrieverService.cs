public interface IRetrieverService
{
    Task<List<DocumentVector>> RetrieveAllDocumentsAsync();
}
