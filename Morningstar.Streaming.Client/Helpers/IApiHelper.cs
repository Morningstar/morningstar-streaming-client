namespace Morningstar.Streaming.Client.Helpers;

public interface IApiHelper
{
    Task<T> ProcessRequestAsync<T>(string requestUri, HttpMethod method, List<KeyValuePair<string, string>>? headers, object? model);
}
