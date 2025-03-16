using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("import", Description = "Import music to your database")]
public class ImportCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("path", 'p', 
        Description = "From the directory.",
        EnvironmentVariable = "IMPORT_PATH",
        IsRequired = true)]
    public required string Path { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new ImportCommandHandler(ConnectionString);
        
        var sortedTopDirectories = Directory
            .EnumerateFileSystemEntries(Path, "*.*", SearchOption.TopDirectoryOnly)
            .OrderBy(dir => dir)
            .ToList();
        
        await Task.WhenAll(
            sortedTopDirectories
                .AsParallel()
                .WithDegreeOfParallelism(8)
                .Select(dir => handler.ProcessDirectoryAsync(dir))
        );
    }
}