using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class DeDuplicateFileCommand
{
    /// <summary>
    /// Check for duplicated music and delete optionally
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="delete">-d, Delete duplicate file.</param>
    [Command("deduplicate")]
    public static void DeDuplicate(string connectionString, string artist = "", bool delete = false)
    {
        var handler = new DeDuplicateFileCommandHandler(connectionString);

        handler.CheckDuplicateFiles(artist, delete);
    }
}