using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("grouptaggingspotifymetadata", Description = "Group Tagging metadata per Album")]
public class GroupTaggingSpotifyMetadataCommand : ICommand
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
        EnvironmentVariable = "GROUPTAGGINGSPOTIFYMETADATA_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "GROUPTAGGINGSPOTIFYMETADATA_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("confirm", 'y', 
        Description = "Always confirm automatically.", 
        IsRequired = false,
        EnvironmentVariable = "GROUPTAGGINGSPOTIFYMETADATA_CONFIRM")]
    public bool Confirm { get; set; } = false;

    [CommandOption("overwrite-tag", 'o', 
        Description = "Overwrite existing tag values.", 
        IsRequired = false,
        EnvironmentVariable = "GROUPTAGGINGSPOTIFYMETADATA_OVERWRITE_TAG")]
    public bool OverwriteTag { get; set; } = true;

    [CommandOption("overwrite-album-tag", 'B', 
        Description = "Overwrite the existing album tag value, only overwrites it if the Album is tagged incorrectly before.", 
        IsRequired = false,
        EnvironmentVariable = "GROUPTAGGINGSPOTIFYMETADATA_OVERWRITE_ALBUM_TAG")]
    public bool OverwriteAlbumTag { get; set; } = true;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new GroupTaggingSpotifyMetadataCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.TagMetadataAsync(Album, OverwriteTag, Confirm, OverwriteAlbumTag);
        }
        else
        {
            await handler.TagMetadataAsync(Artist, Album, OverwriteTag, Confirm, OverwriteAlbumTag);
        }
    }
}