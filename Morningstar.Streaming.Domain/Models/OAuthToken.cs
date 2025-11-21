namespace Morningstar.Streaming.Domain.Models;

public class OAuthToken
{
    public string? Access_Token { get; set; }
    public int? Expires_In { get; set; }
    public string? Token_Type { get; set; }
}
