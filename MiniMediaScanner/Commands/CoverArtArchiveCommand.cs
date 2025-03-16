using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("coverartarchive", Description = "Download Cover art from the Cover Art Archive (only Album cover supported)")]
public class CoverArtArchiveCommand : ICommand
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
        EnvironmentVariable = "COVERARTARCHIVE_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTARCHIVE_ALBUM")]
    public string Album { get; set; }

    [CommandOption("filename", 'f', 
        Description = "File name e.g. cover.jpg.", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTARCHIVE_FILENAME")]
    public string Filename { get; set; } = "cover.jpg";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new CoverArtArchiveCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckAllMissingCoversAsync(Album, Filename);
        }
        else
        {
            await handler.CheckAllMissingCoversAsync(Artist, Album, Filename);
        }
    }
}