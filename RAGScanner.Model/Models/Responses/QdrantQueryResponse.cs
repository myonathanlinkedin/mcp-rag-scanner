public class QdrantQueryResponse
{
    public List<QdrantResult> Result { get; set; }

    public QdrantQueryResponse()
    {
        Result = new List<QdrantResult>();
    }
}

public class QdrantResult
{
    public string Id { get; set; }
    public int Version { get; set; }
    public float Score { get; set; }
    public Payload Payload { get; set; }
    public List<float> Vector { get; set; }
}

public class Payload
{
    public string Url { get; set; }
    public string SourceType { get; set; }
    public string Title { get; set; }
    public DateTime ScrapedAt { get; set; }
}
