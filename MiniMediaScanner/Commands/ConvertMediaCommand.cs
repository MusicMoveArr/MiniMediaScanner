using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class ConvertMediaCommand
{
    /// <summary>
    /// Convert media for example FLAC > M4A
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="fromExtension">-f, From extension.</param>
    /// <param name="toExtension">-t, To extension.</param>
    /// <param name="codec">-c, Codec e.g. aac.</param>
    /// <param name="bitrate">-b, Bitrate e.g. 320k.</param>
    [Command("convert")]
    public static void ConvertMedia(string connectionString,
        string fromExtension, 
        string toExtension, 
        string codec, 
        string bitrate, 
        string artist = "")
    {
        var handler = new ConvertMediaCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.ConvertAllArtists(fromExtension, toExtension, codec, bitrate);
        }
        else
        {
            handler.ConvertByArtist(fromExtension, toExtension, artist, codec, bitrate);
        }
    }
}