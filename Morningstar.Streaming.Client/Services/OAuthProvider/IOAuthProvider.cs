using Morningstar.Streaming.Domain.Models;

namespace Morningstar.Streaming.Client.Services.OAuthProvider;

public interface IOAuthProvider
{
    Task<OAuthSecret> GetOAuthSecretAsync();
}
