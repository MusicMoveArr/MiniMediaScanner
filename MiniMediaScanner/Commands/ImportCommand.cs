using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class ImportCommand
{
    /// <summary>
    /// Import music to your database
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="path">-p, From the directory.</param>
    [Command("import")]
    public static void Import(string connectionString, string path)
    {
        var handler = new ImportCommandHandler(connectionString);
        
        var sortedTopDirectories = Directory
            .EnumerateFileSystemEntries(path, "*.*", SearchOption.TopDirectoryOnly)
            .OrderBy(dir => dir)
            .ToList();
        
        sortedTopDirectories
            .AsParallel()
            .WithDegreeOfParallelism(8)
            .ForAll(dir => handler.ProcessDirectory(dir));
    }
}