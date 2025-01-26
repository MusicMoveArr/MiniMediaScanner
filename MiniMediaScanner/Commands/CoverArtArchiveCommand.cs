using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class CoverArtArchiveCommand
{
    /// <summary>
    /// Download Cover art from the Cover Art Archive
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="filename">-f, File name e.g. cover.jpg.</param>
    [Command("coverartarchive")]
    public static void CoverArtArchive(string connectionString,
        string artist = "",
        string album = "",
        string filename = "cover.jpg")
    {
        var handler = new CoverArtArchiveCommandHandler(connectionString);

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