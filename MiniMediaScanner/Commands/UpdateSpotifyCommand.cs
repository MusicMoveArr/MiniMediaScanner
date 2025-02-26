using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class UpdateSpotifyCommand
{
    /// <summary>
    /// Update MusicBrainz metadata
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artist filter to update.</param>
    /// <param name="spotifyClientId">-SID, Spotify Client Id, to use for the Spotify API.</param>
    /// <param name="spotifySecretId">-SIS, Spotify Secret Id, to use for the Spotify API.</param>
    /// <param name="apiDelay">-D, Api Delay in seconds after each API call to prevent rate limiting.</param>
    [Command("updatespotify")]
    public static void UpdateSpotify(string connectionString, 
        string spotifyClientId,
        string spotifySecretId,
        int apiDelay = 10,
        string artist = "")
    {
        var handler = new UpdateSpotifyCommandHandler(connectionString, spotifyClientId, spotifySecretId, apiDelay);

        if (!string.IsNullOrWhiteSpace(artist))
        {
            handler.UpdateSpotifyArtistsByName(artist);
        }
        else
        {
            handler.UpdateAllSpotifyArtists();
        }
        
    }
}