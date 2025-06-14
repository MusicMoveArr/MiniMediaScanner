using System.Net;

namespace MiniMediaScanner.Models;

public class ProxyModel
{
    public string ProxyUri { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    public WebProxy Proxy => new WebProxy(ProxyUri);

    public NetworkCredential Credentials => new NetworkCredential(Username, Password);

    public ProxyModel(string proxyUri)
    {
        this.ProxyUri = proxyUri;
    }
    public ProxyModel(string proxyUri, string username, string password)
    {
        this.ProxyUri = proxyUri;
        this.Username = username;
        this.Password = password;
    }
}