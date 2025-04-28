public class DocumentMetadata
{
    public string Url { get; set; }
    public string SourceType { get; set; } // "html" or "pdf"
    public string Title { get; set; }
    public string ContentHash { get; set; }
    public DateTime ScrapedAt { get; set; }
}
