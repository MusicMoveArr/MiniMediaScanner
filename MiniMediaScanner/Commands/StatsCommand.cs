using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class StatsCommand
{
    /// <summary>
    /// Get statistics about your media in the database
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    [Command("stats")]
    public static void Stats(string connectionString)
    {
        var handler = new StatsCommandHandler(connectionString);

        handler.ShowStats();
    }
}