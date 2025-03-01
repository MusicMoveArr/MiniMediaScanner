using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class MissingCommand
{
    /// <summary>
    /// Check for missing music
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="provider">-p, Provider can be either MusicBrainz or Spotify.</param>
    [Command("missing")]
    public static void Missing(string connectionString, string artist = "", string provider = "musicbrainz")
    {
        if (string.IsNullOrWhiteSpace(provider) ||
            (provider.ToLower() != "musicbrainz" && artist.ToLower() != "spotify"))
        {
            Console.WriteLine("Provider must be either 'musicbrainz' or 'spotify'");
            return;
        }
        var handler = new MissingCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.CheckAllMissingTracks(provider);
        }
        else
        {
            handler.CheckMissingTracksByArtist(artist, provider);
        }
    }
}