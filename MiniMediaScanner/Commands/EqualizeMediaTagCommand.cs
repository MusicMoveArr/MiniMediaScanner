using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("equalizemediatag", Description = @"Equalize MediaTags of albums from artists to fix issues with albums showing weird/duplicated in Plex/Navidrome etc, 
Tags available: date, originaldate, originalyear, year, disc, asin, catalognumber")]
public class EqualizeMediaTagCommand : ICommand
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
        EnvironmentVariable = "EQUALIZEMEDIATAG_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "EQUALIZEMEDIATAG_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("tag", 't', 
        Description = "Tag.", 
        IsRequired = true,
        EnvironmentVariable = "EQUALIZEMEDIATAG_TAG")]
    public required string Tag { get; init; }

    [CommandOption("confirm", 'y', 
        Description = "Always confirm automatically.", 
        IsRequired = false,
        EnvironmentVariable = "EQUALIZEMEDIATAG_CONFIRM")]
    public bool Confirm { get; set; } = false;
    
    [CommandOption("writetag", 'w', 
        Description = "Tag to write to, if not set, the tag to read from (-t/--tag) is used to write to.", 
        IsRequired = false,
        EnvironmentVariable = "EQUALIZEMEDIATAG_WRITETAG")]
    public string WriteTag { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new EqualizeMediaTagCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(WriteTag))
        {
            WriteTag = Tag;
        }

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.EqualizeTagsAsync(Album, Tag, WriteTag, Confirm);
        }
        else
        {
            await handler.EqualizeTagsAsync(Artist, Album, Tag, WriteTag, Confirm);
        }
    }
}