using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("tagmissingtidalmetadata", Description = "Tag missing metadata using Tidal, optionally write to file")]
public class TagMissingTidalMetadataCommand : ICommand
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
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("write", 'w', 
        Description = "Write missing metadata to media on disk.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_WRITE")]
    public bool Write { get; set; }

    [CommandOption("overwrite-tag", 'o', 
        Description = "Overwrite existing tag values.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_OVERWRITE_TAG")]
    public bool OverwriteTag { get; set; } = true;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new TagMissingTidalMetadataCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.TagMetadataAsync(Write, Album, OverwriteTag);
        }
        else
        {
            await handler.TagMetadataAsync(Write, Artist, Album, OverwriteTag);
        }
    }
}