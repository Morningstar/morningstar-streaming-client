using Morningstar.Snapshot.Client.Services.OAuthProvider;
using Morningstar.Snapshot.Domain.Models;

namespace Morningstar.Snapshot.Client.Sample.Services.OAuthProvider;

public class ExampleOAuthProvider : IOAuthProvider
{
    /// <summary>
    /// Gets the OAuth secret containing username and password.
    /// </summary>
    /// <returns></returns>
    public Task<OAuthSecret> GetOAuthSecretAsync()
    {
        // Add Any custom logic to create and return an OAuthSecret        

        //... implement your custom logic to retrieve credentials ...

        // NB : Avoid hardcoding credentials in production code,
        // This is just an example of the end result after retrieving credentials from a secret store.
        return Task.FromResult(new OAuthSecret
        {
            UserName = "{YOUR_USERNAME}",
            Password = "{YOUR_PASSWORD}"
        });
    }
}
