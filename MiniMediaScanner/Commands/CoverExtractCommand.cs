using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class CoverExtractCommand
{
    /// <summary>
    /// Extract Cover art from the media files
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="filename">-f, File name e.g. cover.jpg.</param>
    [Command("coverextract")]
    public static void CoverExtract(string connectionString,
        string artist = "",
        string album = "",
        string filename = "cover.jpg")
    {
        var handler = new CoverExtractCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.CheckAllMissingCovers(album, filename);
        }
        else
        {
            handler.CheckAllMissingCovers(artist, album, filename);
        }
    }
}