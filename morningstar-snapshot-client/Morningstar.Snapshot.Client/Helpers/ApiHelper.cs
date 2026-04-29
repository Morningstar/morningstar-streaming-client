using System.Text;
using Newtonsoft.Json;
using Morningstar.Snapshot.Domain.Models;

namespace Morningstar.Snapshot.Client.Helpers;

public class ApiHelper : IApiHelper
{
    private HttpClient httpClient { get; }

    public ApiHelper(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Snapshot Client");
    }
    public async Task<T> ProcessRequestAsync<T>(string requestUri, HttpMethod method, List<KeyValuePair<string, string>>? headers, object? model)
    {
        var requestMessage = new HttpRequestMessage(method, requestUri);
        headers?.ForEach(hdr => requestMessage.Headers.Add(hdr.Key, hdr.Value));

        if (model != null)
        {
            var modelData = JsonConvert.SerializeObject(model);
            requestMessage.Content = new StringContent(modelData, Encoding.UTF8, "application/json");
        }
        var response = await httpClient.SendAsync(requestMessage);

        var responseContent = await response.Content.ReadAsStringAsync();
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new IMessageConverter() }
        };

        return JsonConvert.DeserializeObject<T>(responseContent, settings)!;
    }
}
