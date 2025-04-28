public class ApplicationSettings
{
    public ApplicationSettings()
    {
        Api = new ApiSettings();
        Qdrant = new QdrantSettings();
    }

    public string Secret { get; private set; } = default!;
    public string Issuer { get; private set; } = default!;
    public string Audience { get; private set; } = default!;
    public string SignKey { get; private set; } = default!;
    public string KeyId { get; private set; } = default!;
    public int KeyRotationIntervalSeconds { get; set; } = 900; // default 15 minutes
    public int TokenExpirationSeconds { get; set; } = 300; // default 5 minutes

    public ApiSettings Api { get; private set; }
    public QdrantSettings Qdrant { get; private set; }

    public class ApiSettings
    {
        public string ApiKey { get; private set; } = "NO_NEED_IF_USING_LMSTUDIO";
        public string Endpoint { get; private set; } = "http://127.0.0.1:1234/v1";
        public string LlmModel { get; private set; } = "gpt-4o-mini";
    }

    public class QdrantSettings
    {
        public string Endpoint { get; private set; } = "http://localhost:6333";
        public string CollectionName { get; private set; } = "default-collection";
    }
}
