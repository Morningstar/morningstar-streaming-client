namespace Morningstar.Streaming.Client.Services.TokenProvider;

/// <summary>
/// Interface for managing OAuth bearer token creation and caching for API authentication.
/// Handles token fetching, caching, and automatic refresh using credentials from IOAuthProvider.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Creates or retrieves a cached OAuth bearer token for API authentication.
    /// Automatically fetches new token using IOAuthProvider credentials when needed.
    /// </summary>
    /// <returns>Bearer token string to be used in Authorization headers</returns>
    Task<string> CreateBearerTokenAsync();
}
