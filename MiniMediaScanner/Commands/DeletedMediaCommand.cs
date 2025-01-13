using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class DeletedMediaCommand
{
    /// <summary>
    /// Check for deleted/missing music files on disk
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="remove">-r, Remove records from database.</param>
    [Command("missing")]
    public static void DeletedMedia(string connectionString, 
        bool remove = true,
        string artist = "", 
        string album = "")
    {
        var handler = new DeletedMediaCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.CheckAllMissingTracks(remove, album);
        }
        else
        {
            handler.CheckAllMissingTracks(remove, artist, album);
        }
        
    }
}