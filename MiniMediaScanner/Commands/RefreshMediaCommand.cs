using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class RefreshMetadataCommand
{
    /// <summary>
    /// Refresh metadata from files into the database
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    [Command("refreshmetadata")]
    public static void RefreshMetadata(string connectionString,
        string artist = "", 
        string album = "")
    {
        var handler = new RefreshMetadataCommandHandler(connectionString);

        handler.RefreshMetadata(artist, album);
    }
}