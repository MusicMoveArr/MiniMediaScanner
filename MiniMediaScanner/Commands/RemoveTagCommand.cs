using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class RemoveTagCommand
{
    /// <summary>
    /// Remove tags 
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="tag">-t, The tag to remove from media.</param>
    /// <param name="tags">-T, The tags to remove from media.</param>
    /// <param name="confirm">-y, Always confirm automatically.</param>
    [Command("removetag")]
    public static void RemoveTag(string connectionString, 
        string tag = "", 
        string artist = "", 
        string album = "", 
        bool confirm = false,
        List<string> tags = null)
    {
        var handler = new RemoveTagCommandHandler(connectionString);
        if (tags == null)
        {
            tags = new List<string>();
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            tags.Add(tag);
        }

        if (!tags.Any())
        {
            Console.WriteLine("No tags were specified.");
            return;
        }

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.RemoveTagFromMedia(album, tags, confirm);
        }
        else
        {
            handler.RemoveTagFromMedia(artist, album, tags, confirm);
        }
    }
}