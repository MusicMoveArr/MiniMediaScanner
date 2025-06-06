using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("updatemb", Description = "Update MusicBrainz metadata")]
public class UpdateMBCommand : ICommand
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
        EnvironmentVariable = "UPDATEMB_ARTIST")]
    public string Artist { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateMBCommandHandler(ConnectionString);

        if (!string.IsNullOrWhiteSpace(Artist))
        {
            await handler.UpdateMusicBrainzArtistsByNameAsync(Artist);
        }
        else
        {
            await handler.UpdateAllMusicBrainzArtistsAsync();
        }
        
    }
}