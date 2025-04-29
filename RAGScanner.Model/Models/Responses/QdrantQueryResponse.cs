using Newtonsoft.Json;

public class QdrantQueryResponse
{
    [JsonProperty("results")]
    public List<QdrantQueryResult> Results { get; set; }
}

public class QdrantQueryResult
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("vector")]
    public float[] Vector { get; set; }

    [JsonProperty("payload")]
    public Payload Payload { get; set; }
}

public class Payload
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("sourceType")]
    public string SourceType { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("scrapedAt")]
    public string ScrapedAt { get; set; }
}
