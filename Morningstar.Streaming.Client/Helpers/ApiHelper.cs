using System.Text;
using System.Text.Json;

namespace Morningstar.Streaming.Client.Helpers;

public class ApiHelper : IApiHelper
{
    private HttpClient httpClient { get; }

    public ApiHelper(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Canary Client");
    }
    public async Task<T> ProcessRequestAsync<T>(string requestUri, HttpMethod method, List<KeyValuePair<string, string>>? headers, object? model)
    {
        var requestMessage = new HttpRequestMessage(method, requestUri);
        headers?.ForEach(hdr => requestMessage.Headers.Add(hdr.Key, hdr.Value));

        if (model != null)
        {
            var modelData = JsonSerializer.Serialize(model);
            requestMessage.Content = new StringContent(modelData, Encoding.UTF8, "application/json");
        }
        var response = await httpClient.SendAsync(requestMessage);

        return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}
