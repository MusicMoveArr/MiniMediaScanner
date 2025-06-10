using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("updatedeezer", Description = "Update Deezer metadata")]
public class UpdateDeezerCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', 
        Description = "Artist filter to update.", 
        IsRequired = false,
        EnvironmentVariable = "UPDATEDEEZER_ARTIST")]
    public string Artist { get; set; }
    
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateDeezerCommandHandler(ConnectionString);

        if (!string.IsNullOrWhiteSpace(Artist))
        {
            await handler.UpdateDeezerArtistsByNameAsync(Artist);
        }
        else
        {
            await handler.UpdateAllDeezerArtistsAsync();
        }
        
    }
}