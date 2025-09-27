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
    
    [CommandOption("prevent-update-within-days",
        Description = "Prevent updating existing artists within x days from the last pull/update",
        IsRequired = false,
        EnvironmentVariable = "UPDATEMB_PREVENT_UPDATE_WITHIN_DAYS")]
    public int PreventUpdateWithinDays { get; set; } = 7;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateMBCommandHandler(ConnectionString, PreventUpdateWithinDays);

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