namespace Stunsy.SocialGraph.Api.Configuration;

public class GremlinConfiguration
{
    public string Hostname { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
