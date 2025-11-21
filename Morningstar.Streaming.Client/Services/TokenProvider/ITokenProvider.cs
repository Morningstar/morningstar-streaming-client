namespace Morningstar.Streaming.Client.Services.TokenProvider;

public interface ITokenProvider
{
    Task<string> CreateBearerTokenAsync();
}
