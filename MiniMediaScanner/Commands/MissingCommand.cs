using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("missing", Description = "Check for missing music")]
public class MissingCommand : ICommand
{
    private readonly string[] providers = new string[]
    {
        "musicbrainz",
        "spotify",
        "tidal",
        "deezer"
    };

[CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', 
        Description = "Artistname", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_ARTIST")]
    public string Artist { get; set; }

    [CommandOption("provider", 'p', 
        Description = "Provider can be either MusicBrainz or Spotify.", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_PROVIDER")]
    public string Provider { get; set; } = "musicbrainz";
    
    
    [CommandOption("output", 'o', 
        Description = "Output format, tags available: {Artist} {Album} {Track} {ArtistUrl} {AlbumUrl} {TrackUrl}.", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_OUTPUT")]
    public string Output { get; set; } = "{Artist} - {Album} - {Track}";
    
    [CommandOption("filterout", 'F', 
        Description = "Filterout names from the output.", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_FILTEROUT")]
    public List<string>? FilterOut { get; set; } = new List<string>();

    [CommandOption("extension", 'e',
        Description = "When the specific file extension (mp3, opus, wav...) is not found, it's considered missing.",
        IsRequired = false,
        EnvironmentVariable = "MISSING_EXTENSION")]
    public string Extension { get; set; } = string.Empty;
    
    [CommandOption("file", 
        Description = "Save the missing tracks list to a file.", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_FILE")]
    public string FilePath { get; set; }

    [CommandOption("file-append",
        Description = "Append to the file instead of a overwrite.",
        IsRequired = false,
        EnvironmentVariable = "MISSING_FILE_APPEND")]
    public bool FileAppend { get; set; } = false;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Provider) ||
            !providers.Any(p => string.Equals(p, Provider, StringComparison.CurrentCultureIgnoreCase)))
        {
            Console.WriteLine("Provider must be either 'musicbrainz', 'spotify', 'tidal' or 'deezer'.");
            return;
        }
        var handler = new MissingCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckAllMissingTracksAsync(Provider, Output, FilterOut, Extension, FilePath, FileAppend);
        }
        else
        {
            await handler.CheckMissingTracksByArtistAsync(Artist, Provider, Output, FilterOut, Extension, FilePath, FileAppend);
        }
    }
}