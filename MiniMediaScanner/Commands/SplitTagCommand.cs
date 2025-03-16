using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("splittag", Description = "Split the target media tag by the seperator")]
public class SplitTagCommand : ICommand
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
        EnvironmentVariable = "SPLITTAG_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "SPLITTAG_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("tag", 't', 
        Description = "Tag.", 
        IsRequired = true,
        EnvironmentVariable = "SPLITTAG_TAG")]
    public required string Tag { get; init; }
    
    [CommandOption("write-tag", 'w', 
        Description = "Tag to write to, if not set, the tag to read from (-t/--tag) is used to write to.", 
        IsRequired = false,
        EnvironmentVariable = "SPLITTAG_WRITE_TAG")]
    public string Writetag { get; set; }
    
    [CommandOption("update-read-tag", 'r', 
        Description = "Update as well the tag that was being read.", 
        IsRequired = false,
        EnvironmentVariable = "SPLITTAG_UPDATE_READ_TAG")]
    public bool UpdateReadTag { get; set; }
    
    [CommandOption("update-read-tag-original-value", 'R', 
        Description = "Update the read tag with the original tag value.", 
        IsRequired = false,
        EnvironmentVariable = "SPLITTAG_UPDATE_READ_TAG_ORIGINAL_VALUE")]
    public bool UpdateReadTagOriginalValue { get; set; }
    
    [CommandOption("update-write-tag-original-value", 'W', 
        Description = "Update the read tag with the original tag value.", 
        IsRequired = false,
        EnvironmentVariable = "SPLITTAG_UPDATE_WRITE_TAG_ORIGINAL_VALUE")]
    public bool UpdateWriteTagOriginalValue { get; set; }
    
    [CommandOption("confirm", 'y', 
        Description = "Always confirm automatically.", 
        IsRequired = false,
        EnvironmentVariable = "SPLITTAG_CONFIRM")]
    public bool Confirm { get; set; }
    
    [CommandOption("overwrite-tag", 'o', 
        Description = "Overwrite existing tag values.", 
        IsRequired = false,
        EnvironmentVariable = "SPLITTAG_OVERWRITE_TAG")]
    public bool OverWriteTag { get; set; }

    [CommandOption("seperator", 's', 
        Description = "Split seperator.", 
        IsRequired = false,
        EnvironmentVariable = "SPLITTAG_SEPARATOR")]
    public string Seperator { get; set; } = ";";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new SplitTagCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Writetag))
        {
            Writetag = Tag;
        }
        
        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.SplitTagsAsync(Album, Tag, Confirm, Writetag, OverWriteTag, Seperator, UpdateReadTag, UpdateReadTagOriginalValue, UpdateWriteTagOriginalValue);
        }
        else
        {
            await handler.SplitTagsAsync(Artist, Album, Tag, Confirm, Writetag, OverWriteTag, Seperator, UpdateReadTag, UpdateReadTagOriginalValue, UpdateWriteTagOriginalValue);
        }
    }
}