using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class SplitTagCommand
{
    /// <summary>
    /// Split the target media tag by the seperator
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="tag">-t, Tag.</param>
    /// <param name="writetag">-wt, Tag to write to, if not set, the tag to read from (-t/--tag) is used to write to.</param>
    /// <param name="updateReadTag">-rt, Update as well the tag that was being read.</param>
    /// <param name="updateReadTagOriginalValue">-rto, Update the read tag with the original tag value.</param>
    /// <param name="updateWriteTagOriginalValue">-wto, Update the write tag with the original tag value.</param>
    /// <param name="confirm">-y, Always confirm automatically.</param>
    /// <param name="overwriteTag">-ow, Overwrite existing tag values.</param>
    /// <param name="seperator">-s, Split seperator.</param>
    [Command("splittag")]
    public static void SplitTag(string connectionString, 
        string tag,
        string artist = "", 
        string album = "", 
        bool confirm = false,
        string writetag = "",
        string seperator = ";",
        bool overwriteTag = false,
        bool updateReadTag = false,
        bool updateReadTagOriginalValue = false,
        bool updateWriteTagOriginalValue = false)
    {
        var handler = new SplitTagCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(writetag))
        {
            writetag = tag;
        }
        
        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.SplitTags(album, tag, confirm, writetag, overwriteTag, seperator, updateReadTag, updateReadTagOriginalValue, updateWriteTagOriginalValue);
        }
        else
        {
            handler.SplitTags(artist, album, tag, confirm, writetag, overwriteTag, seperator, updateReadTag, updateReadTagOriginalValue, updateWriteTagOriginalValue);
        }
    }
}