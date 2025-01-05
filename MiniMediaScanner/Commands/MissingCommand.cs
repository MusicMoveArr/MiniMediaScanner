using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class MissingCommand
{
    /// <summary>
    /// Check for missing music
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    [Command("missing")]
    public static void Missing(string connectionString, string artist = "")
    {
        var handler = new MissingCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.CheckAllMissingTracks();
        }
        else
        {
            handler.CheckMissingTracksByArtist(artist);
        }
    }
}