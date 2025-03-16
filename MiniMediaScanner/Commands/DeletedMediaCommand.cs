using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("deletedmedia", Description = "Check for deleted/missing music files on disk")]
public class DeletedMediaCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', Description = "Artistname", IsRequired = false)]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', Description = "target Album", IsRequired = false)]
    public string Album { get; set; }

    [CommandOption("remove", 'r', Description = "Remove records from database.", IsRequired = false)]
    public bool Remove { get; set; } = true;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new DeletedMediaCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckAllMissingTracksAsync(Remove, Album);
        }
        else
        {
            await handler.CheckAllMissingTracksAsync(Remove, Artist, Album);
        }
        
    }
}