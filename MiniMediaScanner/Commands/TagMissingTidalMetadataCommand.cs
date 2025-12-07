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
    
    [CommandOption("confirm", 'y', 
        Description = "Always confirm automatically.", 
        IsRequired = false,
        EnvironmentVariable = "GROUPTAGGINGTIDALMETADATA_CONFIRM")]
    public bool Confirm { get; set; } = false;

    [CommandOption("overwrite-tag", 'o', 
        Description = "Overwrite existing tag values.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_OVERWRITE_TAG")]
    public bool OverwriteTag { get; set; } = true;

    [CommandOption("overwrite-artist", 
        Description = "Overwrite the Artist name when tagging from Tidal.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_OVERWRITEARTIST")]
    public bool OverwriteArtist { get; set; }
    
    [CommandOption("overwrite-album-artist", 
        Description = "Overwrite the Album Artist name when tagging from Tidal.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_OVERWRITEALBUMARTIST")]
    public bool OverwriteAlbumArtist { get; set; }
    
    [CommandOption("overwrite-album", 
        Description = "Overwrite the Album name when tagging from Tidal.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_OVERWRITEALBUM")]
    public bool OverwriteAlbum { get; set; }
    
    [CommandOption("overwrite-track", 
        Description = "Overwrite the Track name when tagging from Tidal.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGTIDALMETADATA_OVERWRITETRACK")]
    public bool OverwriteTrack { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new TagMissingTidalMetadataCommandHandler(ConnectionString);
        handler.Confirm = Confirm;
        handler.OverwriteTag = OverwriteTag;
        handler.OverwriteArtist = OverwriteArtist;
        handler.OverwriteAlbumArtist = OverwriteAlbumArtist;
        handler.OverwriteAlbum = OverwriteAlbum;
        handler.OverwriteTrack = OverwriteTrack;

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.TagMetadataAsync();
        }
        else
        {
            await handler.TagMetadataAsync(Artist, Album);
        }
    }
}