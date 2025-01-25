using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class UpdateMBCommand
{
    /// <summary>
    /// Update MusicBrainz metadata
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artist filter to update.</param>
    [Command("updatemb")]
    public static void UpdateMB(string connectionString, string artist = "")
    {
        var handler = new UpdateMBCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.UpdateMusicBrainzArtistsByName(artist);
        }
        else
        {
            handler.UpdateAllMusicBrainzArtists();
        }
        
    }
}