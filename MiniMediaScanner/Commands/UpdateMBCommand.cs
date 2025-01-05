using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class UpdateMBCommand
{
    /// <summary>
    /// Update MusicBrainz metadata
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artistNames">-a, Artists filter to update.</param>
    [Command("updatemb")]
    public static void UpdateMB(string connectionString, List<string>? artistNames = null)
    {
        var handler = new UpdateMBCommandHandler(connectionString);

        if (artistNames?.Count > 0)
        {
            handler.UpdateMusicBrainzArtistsByName(artistNames);
        }
        else
        {
            handler.UpdateAllMusicBrainzArtists();
        }
        
    }
}