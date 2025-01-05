using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class FingerPrintMediaCommand
{
    /// <summary>
    /// Re-fingerprint media
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    [Command("fingerprint")]
    public static void FingerPrintMedia(string connectionString)
    {
        var handler = new FingerPrintMediaCommandHandler(connectionString);

        handler.FingerPrintMedia();
    }
}