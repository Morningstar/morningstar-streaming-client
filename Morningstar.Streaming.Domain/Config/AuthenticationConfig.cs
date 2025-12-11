namespace Morningstar.Streaming.Domain.Config;

public class AuthenticationConfig
{
    public string Audience { get; set; }
    public string Authority { get; set; }
    public List<string> ValidAudiences { get; set; }
    public string ValidIssuer { get; set; }
    public string TokenRequestEndpoint { get; set; }
    public string DeveloperDocumentationUrl { get; set; }
}
