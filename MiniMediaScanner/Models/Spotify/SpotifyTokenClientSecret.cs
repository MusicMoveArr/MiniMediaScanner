using System.Diagnostics;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Models.Spotify;

public class SpotifyTokenClientSecret
{
    public string ClientId { get; private set; }
    public string SecretId { get; private set; }
    public Stopwatch LastUsedTime { get; private set; }
    public SpotifyAuthenticationResponse? AuthenticationResponse { get; set; }
    public SpotifyClient?  SpotifyClient { get; set; }
    public int UseCount { get; set; }
    public DateTime? TooManyRequestsTimeout { get; set; }

    public SpotifyTokenClientSecret(string clientId, string secretId)
    {
        this.ClientId = clientId;
        this.SecretId = secretId;
        this.LastUsedTime = Stopwatch.StartNew();
    }
}