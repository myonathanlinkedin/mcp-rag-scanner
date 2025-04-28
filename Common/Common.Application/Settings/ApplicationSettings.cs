public class ApplicationSettings
{
    public ApplicationSettings() => Secret = default!;

    public string Secret { get; private set; }
    public string Issuer { get; private set; }
    public string Audience { get; private set; }
    public string SignKey    { get; private set; }
    public string KeyId { get; private set; }
    public int KeyRotationIntervalSeconds { get; set; } = 900; // default 15 minutes
    public int TokenExpirationSeconds { get; set; } = 300; // default 5 minutes
}