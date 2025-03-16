using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("fingerprint", Description = "Re-fingerprint media")]
public class FingerPrintMediaCommand : ICommand
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
        EnvironmentVariable = "FINGERPRINT_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "FINGERPRINT_ALBUM")]
    public string Album { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new FingerPrintMediaCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.FingerPrintMediaAsync(Album);
        }
        else
        {
            await handler.FingerPrintMediaAsync(Artist, Album);
        }
    }
}