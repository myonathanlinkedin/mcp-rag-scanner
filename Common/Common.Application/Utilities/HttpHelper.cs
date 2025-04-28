using System.Net.Http.Headers;

public static class HttpHelper
{
    public static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url)
    {
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
