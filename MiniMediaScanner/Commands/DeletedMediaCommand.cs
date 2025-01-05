using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class DeletedMediaCommand
{
    /// <summary>
    /// Check for deleted/missing music files on disk
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="remove">-r, Remove records from database.</param>
    [Command("missing")]
    public static void DeletedMedia(string connectionString, bool remove = true)
    {
        var handler = new DeletedMediaCommandHandler(connectionString);

        handler.CheckAllMissingTracks(remove);
    }
}