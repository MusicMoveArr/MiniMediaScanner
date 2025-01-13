using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class RefreshMetadataCommand
{
    /// <summary>
    /// Refresh metadata from files into the database
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    [Command("refreshmetadata")]
    public static void RefreshMetadata(string connectionString)
    {
        var handler = new RefreshMetadataCommandHandler(connectionString);

        handler.RefreshMetadata();
    }
}