using System.Diagnostics;

namespace MiniMediaScanner.Models.Tidal;

public class TidalTokenClientSecret
{
    public string ClientId { get; private set; }
    public string ClientSecret { get; private set; }
    public Stopwatch LastUsedTime { get; private set; }
    public TidalAuthenticationResponse? AuthenticationResponse { get; set; }
    public int UseCount { get; set; }

    public TidalTokenClientSecret(string clientId, string clientSecret)
    {
        this.ClientId = clientId;
        this.ClientSecret = clientSecret;
        this.LastUsedTime = Stopwatch.StartNew();
    }
}