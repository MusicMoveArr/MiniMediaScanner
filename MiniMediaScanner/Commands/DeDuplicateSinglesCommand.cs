using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class DeDuplicateSinglesCommand
{
    /// <summary>
    /// Check for duplicated music, specifically Singles, and delete if the same song already exists in an album optionally
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="delete">-d, Delete duplicate file.</param>
    [Command("deduplicatesingles")]
    public static void DeDuplicateSingles(string connectionString, string artist = "", bool delete = false)
    {
        var handler = new DeDuplicateSinglesCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.CheckDuplicateFiles(delete);
        }
        else
        {
            handler.CheckDuplicateFiles(artist, delete);
        }
    }
}