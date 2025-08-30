using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Models;
using SmartFormat;

namespace MiniMediaScanner.Commands;

[Command("untaggedfile", Description = "Get a list of untagged files from a artist")]
public class UntaggedCommand : ICommand
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
        EnvironmentVariable = "UNTAGGEDFILES_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "UNTAGGEDFILES_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("providers", 'p', 
        Description = "Providers can be MusicBrainz, Spotify, Tidal, Deezer.", 
        IsRequired = false,
        EnvironmentVariable = "UNTAGGEDFILES_PROVIDERS")]
    public List<string> Providers { get; set; } = ["musicbrainz"];
    
    [CommandOption("output", 'o', 
        Description = "Output format, tags available: {Artist} {Album} {Track} {ArtistUrl} {AlbumUrl} {TrackUrl}.", 
        IsRequired = false,
        EnvironmentVariable = "UNTAGGEDFILES_OUTPUT")]
    public string Output { get; set; } = "{Artist} - {Album} - {Track}";
    
    [CommandOption("filterout", 'F', 
        Description = "Filterout names from the output.", 
        IsRequired = false,
        EnvironmentVariable = "UNTAGGEDFILES_FILTEROUT")]
    public List<string>? FilterOut { get; set; } = new List<string>();
    
    [CommandOption("file", 
        Description = "Save the missing tracks list to a file.", 
        IsRequired = false,
        EnvironmentVariable = "UNTAGGEDFILES_FILE")]
    public string FilePath { get; set; }

    [CommandOption("file-append",
        Description = "Append to the file instead of a overwrite.",
        IsRequired = false,
        EnvironmentVariable = "UNTAGGEDFILES_FILE_APPEND")]
    public bool FileAppend { get; set; } = false;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UntaggedCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.GetUntaggedFilesAsync(Album, Providers, Output,  FilterOut, FilePath, FileAppend);
        }
        else
        {
            await handler.GetUntaggedFilesAsync(Artist, Album, Providers, Output, FilterOut, FilePath, FileAppend);
        }
        
    }
}