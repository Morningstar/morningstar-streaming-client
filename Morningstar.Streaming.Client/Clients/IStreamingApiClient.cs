using Morningstar.Streaming.Domain;

namespace Morningstar.Streaming.Client.Clients
{
    public interface IStreamingApiClient
    {
        /// <summary>
        /// Generic method to create a Level 1 stream with any request type and endpoint.
        /// This allows for flexible endpoint configuration without exposing specific implementation details.
        /// </summary>
        Task<StreamResponse> CreateL1StreamAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class;

        Task SubscribeAsync(string webSocketUrl, Func<string, Task> onMessageAsync, CancellationToken cancellationToken = default);
    }
}
