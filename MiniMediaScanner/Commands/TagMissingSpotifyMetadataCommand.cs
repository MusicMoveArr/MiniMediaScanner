using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("tagmissingspotifymetadata", Description = "Tag missing metadata using Spotify, optionally write to file")]
public class TagMissingSpotifyMetadataCommand : ICommand
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
        EnvironmentVariable = "TAGMISSINGSPOTIFYMETADATA_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGSPOTIFYMETADATA_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("write", 'w', 
        Description = "Write missing metadata to media on disk.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGSPOTIFYMETADATA_WRITE")]
    public bool Write { get; set; }

    [CommandOption("overwrite-tag", 'o', 
        Description = "Overwrite existing tag values.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGSPOTIFYMETADATA_OVERWRITE_TAG")]
    public bool OverwriteTag { get; set; } = true;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new TagMissingSpotifyMetadataCommandHandler(ConnectionString);

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