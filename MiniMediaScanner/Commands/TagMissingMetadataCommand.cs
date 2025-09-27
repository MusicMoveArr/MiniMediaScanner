using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("tagmissingmetadata", Description = "Tag missing metadata using AcousticBrainz, only tries already fingerprinted media, optionally write to file")]
public class TagMissingMetadataCommand : ICommand
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
        EnvironmentVariable = "TAGMISSINGMETADATA_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGMETADATA_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("accoustid", 'A', 
        Description = "AccoustId API Key, required for getting data from MusicBrainz.", 
        IsRequired = true,
        EnvironmentVariable = "TAGMISSINGMETADATA_ACOUSTID")]
    public required string AccoustId { get; init; }
    
    [CommandOption("write", 'w', 
        Description = "Write missing metadata to media on disk.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGMETADATA_WRITE")]
    public bool Write { get; set; }

    [CommandOption("overwrite-tag", 'o', 
        Description = "Overwrite existing tag values.", 
        IsRequired = false,
        EnvironmentVariable = "TAGMISSINGMETADATA_OVERWRITE_TAG")]
    public bool OverwriteTag { get; set; } = true;
    
    [CommandOption("match-percentage",
        Description = "The percentage used for tagging, how accurate it must match with MusicBrainz.",
        EnvironmentVariable = "TAGMISSINGMETADATA_MATCH_PERCENTAGE",
        IsRequired = false)]
    public int MatchPercentage { get; set; } = 80;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new TagMissingMetadataCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.TagMetadataAsync(AccoustId, Write, Album, OverwriteTag, MatchPercentage);
        }
        else
        {
            await handler.TagMetadataAsync(AccoustId, Write, Artist, Album, OverwriteTag, MatchPercentage);
        }
    }
}