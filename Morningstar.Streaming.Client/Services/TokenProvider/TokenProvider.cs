using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.OAuthProvider;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Models;

namespace Morningstar.Streaming.Client.Services.TokenProvider;

public class TokenProvider : ITokenProvider
{
    private readonly ILogger<TokenProvider> logger;
    private readonly AppConfig appConfig;
    private readonly IApiHelper apiHelper;
    private readonly IOAuthProvider oAuthProvider;

    public TokenProvider(ILogger<TokenProvider> logger, IOptions<AppConfig> appConfig, IApiHelper apiHelper, IOAuthProvider oAuthProvider)
    {
        this.logger = logger;
        this.appConfig = appConfig.Value;
        this.apiHelper = apiHelper;
        this.oAuthProvider = oAuthProvider;
    }

    public async Task<string> CreateBearerTokenAsync()
    {
        var oAuthSecret = await oAuthProvider.GetOAuthSecretAsync();
        var headers = new List<KeyValuePair<string, string>>
        {
            new("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{oAuthSecret.UserName}:{oAuthSecret.Password}"))),
        };

        try
        {
            var token = await apiHelper.ProcessRequestAsync<OAuthToken>(appConfig.OAuthAddress!,
                HttpMethod.Post,
                headers,
                null);

            if (token == null)
            {
                logger.LogError("Deserialized OAuth token is null. Please check the secret format.");
            }
            return $"{token!.Token_Type} {token!.Access_Token}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during calling OAuth.");
            throw new InvalidOperationException("Unexpected error during calling OAuth.", ex);
        }
    }
}
