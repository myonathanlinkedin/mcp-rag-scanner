public class ApplicationSettings
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int KeyRotationIntervalSeconds { get; set; }
    public int TokenExpirationSeconds { get; set; }
    public int ResetTokenExpirationSeconds { get; set; }
    public ApiSettings Api { get; set; }
    public QdrantSettings Qdrant { get; set; }
    public SubClassConnectionStrings ConnectionStrings { get; set; }
    public SubLogging Logging { get; set; }
    public SubClassJwtSettings JwtSettings { get; set; }
    public MCPSettings MCP { get; set; }
    public MailHogSettings MailHog { get; set; }
    public string AllowedHosts { get; set; }

    // Nested classes to map sections within the configuration
    public class ApiSettings
    {
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string LlmModel { get; set; }
        public string EmbeddingModel { get; set; }
    }

    public class QdrantSettings
    {
        public string Endpoint { get; set; }
        public string CollectionName { get; set; }
        public float SimilarityThreshold { get; set; }
    }

    public class SubClassConnectionStrings
    {
        public string IdentityDBConnection { get; set; }
        public string RAGDBConnection { get; set; }
    }

    public class SubLogging
    {
        public SubClassLogLevel LogLevel { get; set; }

        public class SubClassLogLevel
        {
            public string Default { get; set; }
            public string Microsoft { get; set; }
            public string MicrosoftHostingLifetime { get; set; }
        }
    }

    public class SubClassJwtSettings
    {
        public string JwksUrl { get; set; }
    }

    public class MCPSettings
    {
        public string ServerName { get; set; }
        public string Endpoint { get; set; }
    }

    public class MailHogSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string FromAddress { get; set; }
    }
}
