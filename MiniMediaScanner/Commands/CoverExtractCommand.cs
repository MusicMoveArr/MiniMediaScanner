using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("coverextract", Description = "Extract Cover art from the media files (only Album cover supported)")]
public class CoverExtractCommand : ICommand
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

    [CommandOption("filename", 'f', Description = "File name e.g. cover.jpg.", IsRequired = false)]
    public string Filename { get; set; } = "cover.jpg";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new CoverExtractCommandHandler(ConnectionString);

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