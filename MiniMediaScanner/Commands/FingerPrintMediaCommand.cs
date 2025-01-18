using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class FingerPrintMediaCommand
{
    /// <summary>
    /// Re-fingerprint media
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    [Command("fingerprint")]
    public static void FingerPrintMedia(string connectionString,
        string artist = "", 
        string album = "")
    {
        var handler = new FingerPrintMediaCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.FingerPrintMedia(album);
        }
        else
        {
            handler.FingerPrintMedia(artist, album);
        }
    }
}