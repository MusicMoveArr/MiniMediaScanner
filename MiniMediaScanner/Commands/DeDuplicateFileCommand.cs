using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("deduplicate", Description = "Check for duplicated music and delete optionally")]
public class DeDuplicateFileCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', 
        Description = "Artistname", 
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("delete", 'd', 
        Description = "Delete duplicate file", 
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_DELETE")]
    public bool Delete { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new DeDuplicateFileCommandHandler(ConnectionString);

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