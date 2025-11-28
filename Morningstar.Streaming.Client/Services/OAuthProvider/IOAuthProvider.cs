using Morningstar.Streaming.Domain.Models;

namespace Morningstar.Streaming.Client.Services.OAuthProvider;

/// <summary>
/// Interface for providing OAuth credentials to authenticate with the Morningstar Streaming API.
/// Implementations should securely retrieve credentials from configuration or secret stores.
/// </summary>
public interface IOAuthProvider
{
    /// <summary>
    /// Gets the OAuth credentials (username and password) for API authentication.
    /// </summary>
    /// <returns>OAuthSecret containing username and password</returns>
    Task<OAuthSecret> GetOAuthSecretAsync();
}
