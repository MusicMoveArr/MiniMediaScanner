using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class EqualizeMediaTagCommand
{
    /// <summary>
    /// Equalize MediaTags of albums from artists to fix issues with albums showing weird/duplicated in Plex/Navidrome etc
    /// Tags available: date
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="tag">-t, Tag.</param>
    /// <param name="confirm">-y, Always confirm automatically.</param>
    [Command("equalizemediatag")]
    public static void EqualizeMediaTag(string connectionString, 
        string tag,
        string artist = "", 
        string album = "", 
        bool confirm = false)
    {
        var handler = new EqualizeMediaTagCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.EqualizeTags(album, tag, confirm);
        }
        else
        {
            handler.EqualizeTags(artist, album, tag, confirm);
        }
    }
}