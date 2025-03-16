using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("refreshmetadata", Description = "Refresh metadata from files into the database")]
public class RefreshMetadataCommand : ICommand
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
        EnvironmentVariable = "REFRESHMETADATA_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "REFRESHMETADATA_ALBUM")]
    public string Album { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new RefreshMetadataCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.RefreshMetadataAsync(Album);
        }
        else
        {
            await handler.RefreshMetadataAsync(Artist, Album);
        }
    }
}