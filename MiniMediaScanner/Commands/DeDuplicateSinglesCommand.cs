using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("deduplicatesingles", Description = "Check for duplicated music, specifically Singles, and delete if the same song already exists in an album optionally")]
public class DeDuplicateSinglesCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', Description = "Artistname", IsRequired = false)]
    public string Artist { get; set; }
    
    [CommandOption("delete", 'd', Description = "Delete duplicate file", IsRequired = false)]
    public bool Delete { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new DeDuplicateSinglesCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckDuplicateFilesAsync(Delete);
        }
        else
        {
            await handler.CheckDuplicateFilesAsync(Artist, Delete);
        }
    }
}