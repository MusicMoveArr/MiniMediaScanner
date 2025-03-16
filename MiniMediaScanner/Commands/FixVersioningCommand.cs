using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("fixversioning", Description = @"Find media that are using the same track/disc numbering, usually normal version and (AlbumVersion), (Live) etc. 
The media with the longest file name and contains TrackFilters will get incremented disc number. 
This will make it so the normal version of the album stays at disc 1 but remix(etc) gets disc number 1001+")]
public class FixVersioningCommand : ICommand
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
        EnvironmentVariable = "FIXVERSIONING_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "FIXVERSIONING_ALBUM")]
    public string Album { get; set; }

    [CommandOption("disc-increment", 'd', 
        Description = "Disc increment for remixes (+1000 recommended).", 
        IsRequired = false,
        EnvironmentVariable = "FIXVERSIONING_DISC_INCREMENT")]
    public int DiscIncrement { get; set; } = 1000;
    
    [CommandOption("track-filters", 'f', 
        Description = "Filter names to apply to tracks, .e.g. (remixed by ...).", 
        IsRequired = false,
        EnvironmentVariable = "FIXVERSIONING_TRACK_FILTERS")]
    public List<string> TrackFilters { get; set; }
    
    [CommandOption("confirm", 'y', 
        Description = "Always confirm automatically.", 
        IsRequired = false,
        EnvironmentVariable = "FIXVERSIONING_CONFIRM")]
    public bool Confirm { get; set; } = false;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (TrackFilters == null)
        {
            TrackFilters = new List<string>();
        }
        
        var handler = new FixVersioningCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.FixDiscVersioningAsync(Album, DiscIncrement, TrackFilters, Confirm);
        }
        else
        {
            await handler.FixDiscVersioningAsync(Artist, Album, DiscIncrement, TrackFilters, Confirm);
        }
    }
}