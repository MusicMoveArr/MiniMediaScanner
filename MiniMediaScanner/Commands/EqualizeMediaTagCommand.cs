using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class EqualizeMediaTagCommand
{
    /// <summary>
    /// Equalize MediaTags of albums from artists to fix issues with albums showing weird/duplicated in Plex/Navidrome etc
    /// Tags available: date, originaldate, originalyear, year, disc, asin, catalognumber
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="tag">-t, Tag.</param>
    /// <param name="writetag">-wt, Tag to write to, if not set, the tag to read from (-t/--tag) is used to write to.</param>
    /// <param name="confirm">-y, Always confirm automatically.</param>
    [Command("equalizemediatag")]
    public static void EqualizeMediaTag(string connectionString, 
        string tag,
        string artist = "", 
        string album = "", 
        bool confirm = false,
        string writetag = "")
    {
        var handler = new EqualizeMediaTagCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(writetag))
        {
            writetag = tag;
        }

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.EqualizeTags(album, tag, writetag, confirm);
        }
        else
        {
            handler.EqualizeTags(artist, album, tag, writetag, confirm);
        }
    }
}