using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class FixVersioningCommand
{
    /// <summary>
    /// Find media that are using the same track/disc numbering, usually normal version and (AlbumVersion), (Live) etc
    /// The media with the longest file name and contains TrackFilters will get incremented disc number
    /// This will make it so the normal version of the album stays at disc 1 but remix(etc) gets disc number 1001+
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="discincrement">-d, Disc increment for remixes (+1000 recommended).</param>
    /// <param name="trackFilters">-f, Filter names to apply to tracks, .e.g. (remixed by ...).</param>
    /// <param name="confirm">-y, Always confirm automatically.</param>
    [Command("equalizemediatag")]
    public static void FixVersioning(string connectionString, 
        string artist = "", 
        string album = "", 
        int discincrement = 1000,
        List<string> trackFilters = null, 
        bool confirm = false)
    {
        if (trackFilters == null)
        {
            trackFilters = new List<string>();
        }
        
        var handler = new FixVersioningCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.FixDiscVersioning(album, discincrement, trackFilters, confirm);
        }
        else
        {
            handler.FixDiscVersioning(artist, album, discincrement, trackFilters, confirm);
        }
    }
}