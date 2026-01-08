namespace Morningstar.Streaming.Domain.Config
{
    public class AuthenticationConfig
    {
        public string Audience { get; set; } = null!;
        public string Authority { get; set; } = null!;
        public List<string> ValidAudiences { get; set; } = null!;
        public string ValidIssuer { get; set; } = null!;
        public string TokenRequestEndpoint { get; set; } = null!;
        public string DeveloperDocumentationUrl { get; set; } = null!;
    }
}
